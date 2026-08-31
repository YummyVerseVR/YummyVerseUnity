#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// Win32 の通信APIだけで作った最小のシリアルポート。
    ///
    /// 同期(非オーバーラップ)ハンドルを使い、読み取りの上限時間は COMMTIMEOUTS に任せる。
    /// これにより Read は「データがあれば即時、無ければ指定ミリ秒で 0 を返す」挙動になり、
    /// 受信スレッドを1本回すだけで送受信の両方を捌ける。
    /// </summary>
    internal sealed class WindowsSerialPort : ISerialPortConnection
    {
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint OpenExisting = 3;
        private const uint MaxDword = 0xFFFFFFFF;
        private const uint PurgeTxClear = 0x0004;
        private const uint PurgeRxClear = 0x0008;
        private const int ErrorInsufficientBuffer = 122;

        private static readonly IntPtr InvalidHandle = new(-1);

        private IntPtr _handle;

        public string PortName { get; }

        public WindowsSerialPort(string portName, SerialPortSettings settings)
        {
            PortName = portName;

            // COM10 以降は "\\.\" 接頭辞が無いと開けない。COM1〜9 でも同じ書式で問題ない。
            _handle = CreateFileW(
                @"\\.\" + portName, GenericRead | GenericWrite, 0, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);

            if (_handle == InvalidHandle)
            {
                _handle = IntPtr.Zero;
                throw new IOException($"{portName} を開けませんでした (error {Marshal.GetLastWin32Error()})");
            }

            try
            {
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
            EnsureOpen();

            var target = offset == 0 ? buffer : new byte[count];
            if (!ReadFile(_handle, target, (uint)count, out var read, IntPtr.Zero))
            {
                throw new IOException($"{PortName} の読み取りに失敗しました (error {Marshal.GetLastWin32Error()})");
            }

            if (offset != 0 && read > 0) Buffer.BlockCopy(target, 0, buffer, offset, (int)read);
            return (int)read;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            EnsureOpen();

            var source = buffer;
            if (offset != 0)
            {
                source = new byte[count];
                Buffer.BlockCopy(buffer, offset, source, 0, count);
            }

            if (!WriteFile(_handle, source, (uint)count, out var written, IntPtr.Zero) || written != count)
            {
                throw new IOException($"{PortName} への書き込みに失敗しました (error {Marshal.GetLastWin32Error()})");
            }
        }

        public void DiscardBuffers()
        {
            if (_handle == IntPtr.Zero) return;
            PurgeComm(_handle, PurgeRxClear | PurgeTxClear);
        }

        private void EnsureOpen()
        {
            if (_handle == IntPtr.Zero) throw new IOException($"{PortName} は閉じられています");
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero) return;

            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }

        /// <summary>
        /// DOS デバイス名を総なめして COM ポートだけを拾う。
        /// レジストリ (SERIALCOMM) を読む方法もあるが、Microsoft.Win32.Registry は
        /// プレイヤー側のプロファイルに無いためこちらを使う。
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

            // COM10 が COM2 より前に来ないよう、番号順に並べる。
            names.Sort((a, b) => ComPortIndex(a).CompareTo(ComPortIndex(b)));
            return names;
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

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileW(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

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
            IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteFile(
            IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDeviceW(string lpDeviceName, IntPtr lpTargetPath, uint ucchMax);
    }
}
#endif
