#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// Win32 の通信APIだけで作った最小のシリアルポート。
    ///
    /// オーバーラップ(非同期)ハンドルを使い、読み取りの上限時間は COMMTIMEOUTS に任せる。
    /// これにより Read は「データがあれば即時、無ければ指定ミリ秒で 0 を返す」挙動になり、
    /// 受信スレッドを1本回すだけで送受信の両方を捌ける。
    ///
    /// 同期ハンドルではなくオーバーラップにしてあるのは、USB が抜けた瞬間に
    /// 保留中の ReadFile が戻ってこなくなる場合があるためである。
    /// 同期ハンドルで別スレッドから CloseHandle するのは Windows では未定義動作で、
    /// PCVR 運用中に Quest Link を抜き差しすると USB が再列挙され、この窓が実際に開く。
    /// オーバーラップなら <see cref="CancelPendingIo"/> (CancelIoEx) で確実に中断できる。
    ///
    /// マネージド配列を直接 ReadFile へ渡さないのも同じ理由による。非同期 I/O の完了は
    /// P/Invoke の呼び出しより後で、その間に GC が配列を動かしうるため、
    /// 送受信バッファは未管理メモリに置いてから copy している。
    ///
    /// スレッドの約束: 生成・Read・Write・DiscardBuffers・Dispose は受信スレッドだけが呼ぶ。
    /// 例外は <see cref="CancelPendingIo"/> のみで、これだけは他スレッドから呼んでよい。
    /// この約束があるので「保留中の I/O を抱えたまま OVERLAPPED を解放する」経路が存在しない。
    /// </summary>
    internal sealed class WindowsSerialPort : ISerialPortConnection
    {
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint OpenExisting = 3;
        private const uint FileFlagOverlapped = 0x40000000;
        private const uint MaxDword = 0xFFFFFFFF;
        private const uint PurgeTxClear = 0x0004;
        private const uint PurgeRxClear = 0x0008;

        private const int ErrorFileNotFound = 2;
        private const int ErrorAccessDenied = 5;
        private const int ErrorInvalidHandle = 6;
        private const int ErrorBadCommand = 22;
        private const int ErrorInsufficientBuffer = 122;
        private const int ErrorDeviceNotConnected = 1167;
        private const int ErrorOperationAborted = 995;
        private const int ErrorIoPending = 997;

        private static readonly IntPtr InvalidHandle = new(-1);

        private IntPtr _handle;

        private IntPtr _readEvent;
        private IntPtr _writeEvent;
        private IntPtr _readOverlapped;
        private IntPtr _writeOverlapped;

        private IntPtr _readBuffer;
        private int _readBufferSize;
        private IntPtr _writeBuffer;
        private int _writeBufferSize;

        public string PortName { get; }

        public WindowsSerialPort(string portName, SerialPortSettings settings)
        {
            PortName = portName;

            // COM10 以降は "\\.\" 接頭辞が無いと開けない。COM1〜9 でも同じ書式で問題ない。
            var handle = CreateFileW(
                @"\\.\" + portName, GenericRead | GenericWrite, 0, IntPtr.Zero, OpenExisting,
                FileFlagOverlapped, IntPtr.Zero);

            if (handle == InvalidHandle)
            {
                throw new IOException($"{portName} を開けませんでした (error {Marshal.GetLastWin32Error()})");
            }

            _handle = handle;

            try
            {
                // 手動リセットにするのは、GetOverlappedResult が待つ前に完了しても取りこぼさないため。
                _readEvent = CreateEventW(IntPtr.Zero, true, false, null);
                _writeEvent = CreateEventW(IntPtr.Zero, true, false, null);
                if (_readEvent == IntPtr.Zero || _writeEvent == IntPtr.Zero)
                {
                    throw new IOException($"{portName} の完了イベントを作れませんでした (error {Marshal.GetLastWin32Error()})");
                }

                var overlappedSize = Marshal.SizeOf<CommOverlapped>();
                _readOverlapped = Marshal.AllocHGlobal(overlappedSize);
                _writeOverlapped = Marshal.AllocHGlobal(overlappedSize);

                Configure(settings);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>115200 8N1 / フロー制御なし (プロトコル仕様書 §4)。</summary>
        private void Configure(SerialPortSettings settings)
        {
            var dcb = new Dcb { DCBlength = (uint)Marshal.SizeOf<Dcb>() };
            if (!GetCommState(_handle, ref dcb))
            {
                throw new IOException($"{PortName} の通信設定を取得できませんでした (error {Marshal.GetLastWin32Error()})");
            }

            dcb.BaudRate = (uint)settings.BaudRate;
            dcb.ByteSize = 8;
            dcb.Parity = 0;   // NOPARITY
            dcb.StopBits = 0; // ONESTOPBIT

            // fBinary | fDtrControl=DTR_CONTROL_ENABLE | fRtsControl=RTS_CONTROL_ENABLE。
            // USB CDC のマイコンは DTR が立つまで送信を始めない実装が多いので必ず立てる。
            // DTR でリセットが掛かるボードもあるが、HELLO を再送するので取りこぼさない (仕様書 §7.1)。
            dcb.Flags = 0x1 | (1u << 4) | (1u << 12);

            if (!SetCommState(_handle, ref dcb))
            {
                throw new IOException($"{PortName} の通信設定を適用できませんでした (error {Marshal.GetLastWin32Error()})");
            }

            // ReadInterval と Multiplier を MAXDWORD にすると、
            // 「バッファにあるぶんを即返す。空なら Constant ミリ秒だけ最初の1バイトを待つ」になる。
            var timeouts = new CommTimeouts
            {
                ReadIntervalTimeout = MaxDword,
                ReadTotalTimeoutMultiplier = MaxDword,
                ReadTotalTimeoutConstant = (uint)settings.ReadTimeoutMilliseconds,
                WriteTotalTimeoutMultiplier = 0,
                WriteTotalTimeoutConstant = 1000
            };

            if (!SetCommTimeouts(_handle, ref timeouts))
            {
                throw new IOException($"{PortName} のタイムアウトを設定できませんでした (error {Marshal.GetLastWin32Error()})");
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var handle = EnsureOpen();
            EnsureBuffer(ref _readBuffer, ref _readBufferSize, count);

            ResetEvent(_readEvent);
            PrepareOverlapped(_readOverlapped, _readEvent);

            if (!ReadFile(handle, _readBuffer, (uint)count, IntPtr.Zero, _readOverlapped))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ErrorIoPending) throw ReadFailure(error);
            }

            // COMMTIMEOUTS が効くので、この待ちは必ず有限時間で戻る。
            // CancelIoEx で中断された場合も ERROR_OPERATION_ABORTED として戻ってくる。
            if (!GetOverlappedResult(handle, _readOverlapped, out var read, true))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ErrorOperationAborted) return 0;

                throw ReadFailure(error);
            }

            if (read > 0) Marshal.Copy(_readBuffer, buffer, offset, (int)read);
            return (int)read;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            var handle = EnsureOpen();
            EnsureBuffer(ref _writeBuffer, ref _writeBufferSize, count);
            Marshal.Copy(buffer, offset, _writeBuffer, count);

            ResetEvent(_writeEvent);
            PrepareOverlapped(_writeOverlapped, _writeEvent);

            if (!WriteFile(handle, _writeBuffer, (uint)count, IntPtr.Zero, _writeOverlapped))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ErrorIoPending) throw WriteFailure(error);
            }

            if (!GetOverlappedResult(handle, _writeOverlapped, out var written, true))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ErrorOperationAborted)
                {
                    throw new IOException($"{PortName} への書き込みが中断されました");
                }

                throw WriteFailure(error);
            }

            if (written != count)
            {
                throw new IOException($"{PortName} へ {count} バイト中 {written} バイトしか書き込めませんでした");
            }
        }

        public void DiscardBuffers()
        {
            var handle = Volatile.Read(ref _handle);
            if (handle == IntPtr.Zero) return;

            PurgeComm(handle, PurgeRxClear | PurgeTxClear);
        }

        /// <summary>
        /// 受信スレッド以外から呼ばれうる唯一のメソッド。
        /// CancelIoEx は同一プロセス内であれば、どのスレッドが発行した I/O でも打ち切れる。
        /// </summary>
        public void CancelPendingIo()
        {
            var handle = Volatile.Read(ref _handle);
            if (handle == IntPtr.Zero) return;

            // 相手が既に消えていれば失敗するが、その場合は打ち切る対象も無いので黙って諦める。
            CancelIoEx(handle, IntPtr.Zero);
        }

        private IntPtr EnsureOpen()
        {
            var handle = Volatile.Read(ref _handle);
            if (handle == IntPtr.Zero) throw new IOException($"{PortName} は閉じられています");

            return handle;
        }

        public void Dispose()
        {
            var handle = Volatile.Read(ref _handle);
            if (handle != IntPtr.Zero)
            {
                // 閉じる前に必ず打ち切る。保留中の I/O を抱えたまま CloseHandle すると
                // ドライバ次第で待たされる。
                CancelIoEx(handle, IntPtr.Zero);
                Volatile.Write(ref _handle, IntPtr.Zero);
                CloseHandle(handle);
            }

            FreeHandle(ref _readEvent, CloseHandle);
            FreeHandle(ref _writeEvent, CloseHandle);

            FreeUnmanaged(ref _readOverlapped);
            FreeUnmanaged(ref _writeOverlapped);

            FreeUnmanaged(ref _readBuffer);
            _readBufferSize = 0;
            FreeUnmanaged(ref _writeBuffer);
            _writeBufferSize = 0;
        }

        // ------------------------------------------------------------------
        // 補助
        // ------------------------------------------------------------------

        private static void PrepareOverlapped(IntPtr overlapped, IntPtr completionEvent) =>
            Marshal.StructureToPtr(new CommOverlapped { EventHandle = completionEvent }, overlapped, false);

        private static void EnsureBuffer(ref IntPtr buffer, ref int size, int required)
        {
            if (required <= 0) throw new ArgumentOutOfRangeException(nameof(required));
            if (buffer != IntPtr.Zero && size >= required) return;

            FreeUnmanaged(ref buffer);
            buffer = Marshal.AllocHGlobal(required);
            size = required;
        }

        private static void FreeUnmanaged(ref IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return;

            Marshal.FreeHGlobal(pointer);
            pointer = IntPtr.Zero;
        }

        private static void FreeHandle(ref IntPtr handle, Func<IntPtr, bool> close)
        {
            if (handle == IntPtr.Zero) return;

            close(handle);
            handle = IntPtr.Zero;
        }

        private IOException ReadFailure(int error) =>
            new($"{PortName} の読み取りに失敗しました (error {error}{DeviceLostHint(error)})");

        private IOException WriteFailure(int error) =>
            new($"{PortName} への書き込みに失敗しました (error {error}{DeviceLostHint(error)})");

        /// <summary>
        /// USB が抜かれた/再列挙されたときに出るコード。ログを読む側が
        /// 「不具合」と「ケーブルが抜けただけ」を切り分けられるよう注記する。
        /// </summary>
        private static string DeviceLostHint(int error) =>
            error is ErrorFileNotFound or ErrorAccessDenied or ErrorInvalidHandle
                or ErrorBadCommand or ErrorDeviceNotConnected
                ? ": デバイスが取り外されました"
                : string.Empty;

        // ------------------------------------------------------------------
        // 列挙
        // ------------------------------------------------------------------

        /// <summary>
        /// DOS デバイス名を総なめして COM ポートだけを拾う。
        /// レジストリ (SERIALCOMM) を読む方法もあるが、Microsoft.Win32.Registry は
        /// プレイヤー側のプロファイルに無いためこちらを使う。
        ///
        /// 並び順は「USB CDC → その他 → Bluetooth」。咀嚼計は USB CDC なので当たりが早くなり、
        /// CreateFile が数秒ブロックすることのある Bluetooth の仮想ポートを後回しにできる。
        /// 仕様書 §6.2 のとおり、候補から外すことはしない。
        /// </summary>
        public static IReadOnlyList<string> ListPortNames()
        {
            var size = 1 << 16;

            for (var attempt = 0; attempt < 4; attempt++)
            {
                var buffer = Marshal.AllocHGlobal(size * sizeof(char));
                try
                {
                    var written = QueryDosDeviceW(null, buffer, (uint)size);
                    if (written == 0)
                    {
                        if (Marshal.GetLastWin32Error() != ErrorInsufficientBuffer) return Array.Empty<string>();

                        size *= 4;
                        continue;
                    }

                    return ParseDeviceNames(buffer, (int)written);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            return Array.Empty<string>();
        }

        /// <summary>QueryDosDevice の戻りは NUL 区切りで最後が空文字の連結文字列。</summary>
        private static IReadOnlyList<string> ParseDeviceNames(IntPtr buffer, int charCount)
        {
            var names = new List<string>();
            var start = 0;

            for (var i = 0; i < charCount; i++)
            {
                if (Marshal.ReadInt16(buffer, i * sizeof(char)) != 0) continue;

                if (i > start)
                {
                    var name = Marshal.PtrToStringUni(IntPtr.Add(buffer, start * sizeof(char)), i - start);
                    if (IsComPortName(name)) names.Add(name);
                }

                start = i + 1;
            }

            // 種別の判定はポートごとに1回だけ行う。比較のたびに引くと
            // ポート数ぶんの二乗に近い回数 QueryDosDevice を叩くことになる。
            var ranked = new List<KeyValuePair<string, int>>(names.Count);
            foreach (var name in names) ranked.Add(new KeyValuePair<string, int>(name, DeviceKindRank(name)));

            // 種別が同じものどうしは COM10 が COM2 より前に来ないよう番号順に並べる。
            ranked.Sort((a, b) =>
            {
                var byKind = a.Value.CompareTo(b.Value);
                return byKind != 0 ? byKind : ComPortIndex(a.Key).CompareTo(ComPortIndex(b.Key));
            });

            var ordered = new List<string>(ranked.Count);
            foreach (var entry in ranked) ordered.Add(entry.Key);

            return ordered;
        }

        /// <summary>
        /// COM 名の裏にある NT デバイス名から種別を推定する。ポートは開かないので副作用が無い。
        /// USB CDC は \Device\USBSERn、Bluetooth の SPP は \Device\BthModemn などになる。
        /// </summary>
        private static int DeviceKindRank(string portName)
        {
            var target = QueryDosTarget(portName);
            if (string.IsNullOrEmpty(target)) return 1;

            if (target.IndexOf("USBSER", StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (target.IndexOf("BTH", StringComparison.OrdinalIgnoreCase) >= 0) return 2;

            return 1;
        }

        private static string QueryDosTarget(string portName)
        {
            const int size = 1024;

            var buffer = Marshal.AllocHGlobal(size * sizeof(char));
            try
            {
                var written = QueryDosDeviceW(portName, buffer, size);
                return written == 0 ? null : Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool IsComPortName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 3) return false;
            if (!name.StartsWith("COM", StringComparison.Ordinal)) return false;

            for (var i = 3; i < name.Length; i++)
            {
                if (name[i] < '0' || name[i] > '9') return false;
            }

            return true;
        }

        private static int ComPortIndex(string name) =>
            int.TryParse(name.Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                ? index
                : int.MaxValue;

        // ------------------------------------------------------------------
        // Win32
        // ------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        private struct Dcb
        {
            public uint DCBlength;
            public uint BaudRate;
            public uint Flags;
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public sbyte XonChar;
            public sbyte XoffChar;
            public sbyte ErrorChar;
            public sbyte EofChar;
            public sbyte EvtChar;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CommTimeouts
        {
            public uint ReadIntervalTimeout;
            public uint ReadTotalTimeoutMultiplier;
            public uint ReadTotalTimeoutConstant;
            public uint WriteTotalTimeoutMultiplier;
            public uint WriteTotalTimeoutConstant;
        }

        /// <summary>OVERLAPPED。シリアルは位置を持たないので Offset は常に 0 のままでよい。</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct CommOverlapped
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr EventHandle;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileW(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEventW(
            IntPtr lpEventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bManualReset,
            [MarshalAs(UnmanagedType.Bool)] bool bInitialState,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCommState(IntPtr hFile, ref Dcb lpDcb);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCommState(IntPtr hFile, ref Dcb lpDcb);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCommTimeouts(IntPtr hFile, ref CommTimeouts lpCommTimeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PurgeComm(IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadFile(
            IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, IntPtr lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteFile(
            IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, IntPtr lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOverlappedResult(
            IntPtr hFile, IntPtr lpOverlapped, out uint lpNumberOfBytesTransferred,
            [MarshalAs(UnmanagedType.Bool)] bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CancelIoEx(IntPtr hFile, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDeviceW(string lpDeviceName, IntPtr lpTargetPath, uint ucchMax);
    }
}
#endif
