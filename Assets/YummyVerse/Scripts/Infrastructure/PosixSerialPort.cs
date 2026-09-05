#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// libc の termios だけで作った最小のシリアルポート。エディタでの動作確認用。
    ///
    /// VMIN=0 / VTIME=読み取りタイムアウト に設定してあるため、read(2) は
    /// 「データがあれば即時、無ければ指定時間で 0 を返す」挙動になる。Windows 実装と同じ契約。
    ///
    /// termios は構造体レイアウトも定数値も macOS と Linux で違うので、
    /// マネージド側では未管理バッファとして扱い、必要なフィールドだけをオフセットで叩く。
    /// </summary>
    internal sealed class PosixSerialPort : ISerialPortConnection
    {
        private const string Libc = "libc";

        private const int ORdWr = 0x0002;
        private const int FGetFl = 3;
        private const int FSetFl = 4;
        private const int TcsaNow = 0;
        private const uint Ignpar = 0x0004;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        // macOS: tcflag_t / speed_t は 8 バイト、NCCS は 20。
        private const int TermiosSize = 72;
        private const int OffsetIFlag = 0;
        private const int OffsetOFlag = 8;
        private const int OffsetCFlag = 16;
        private const int OffsetLFlag = 24;
        private const int OffsetControlChars = 32;
        private const int Vmin = 16;
        private const int Vtime = 17;
        private const uint Cs8 = 0x00000300;
        private const uint Cread = 0x00000800;
        private const uint Clocal = 0x00008000;
        private const int ONoctty = 0x00020000;
        private const int ONonblock = 0x00000004;
        private const int Tciflush = 1;
        private const ulong Baud115200 = 115200;
#else
        // Linux: tcflag_t / speed_t は 4 バイト、c_line を挟んで NCCS は 32。
        private const int TermiosSize = 60;
        private const int OffsetIFlag = 0;
        private const int OffsetOFlag = 4;
        private const int OffsetCFlag = 8;
        private const int OffsetLFlag = 12;
        private const int OffsetControlChars = 17;
        private const int Vmin = 6;
        private const int Vtime = 5;
        private const uint Cs8 = 0x00000030;
        private const uint Cread = 0x00000080;
        private const uint Clocal = 0x00000800;
        private const int ONoctty = 0x00000100;
        private const int ONonblock = 0x00000800;
        private const int Tciflush = 0;
        private const ulong Baud115200 = 0x1002;
#endif

        private int _fd = -1;

        public string PortName { get; }

        public PosixSerialPort(string portName, SerialPortSettings settings)
        {
            PortName = portName;

            // O_NONBLOCK 付きで開かないと、キャリア検出待ちで open(2) が止まる端末がある。
            _fd = open(portName, ORdWr | ONoctty | ONonblock);
            if (_fd < 0)
            {
                _fd = -1;
                throw new IOException($"{portName} を開けませんでした (errno {Marshal.GetLastWin32Error()})");
            }

            try
            {
                // 設定は普通のブロッキング read で行う。待ち時間は VTIME で決める。
                var flags = fcntl(_fd, FGetFl, 0);
                if (flags < 0 || fcntl(_fd, FSetFl, flags & ~ONonblock) < 0)
                {
                    throw new IOException($"{portName} をブロッキングモードへ戻せませんでした");
                }

                Configure(settings);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>115200 8N1 / フロー制御なし / raw モード (プロトコル仕様書 §4)。</summary>
        private void Configure(SerialPortSettings settings)
        {
            var termios = Marshal.AllocHGlobal(TermiosSize);
            try
            {
                for (var i = 0; i < TermiosSize; i++) Marshal.WriteByte(termios, i, 0);

                if (tcgetattr(_fd, termios) != 0)
                {
                    throw new IOException($"{PortName} の termios を取得できませんでした (errno {Marshal.GetLastWin32Error()})");
                }

                // 端末としての加工 (エコー・改行変換・シグナル生成) を全部止めて素のバイト列にする。
                WriteFlag(termios, OffsetIFlag, Ignpar);
                WriteFlag(termios, OffsetOFlag, 0);
                WriteFlag(termios, OffsetLFlag, 0);
                WriteFlag(termios, OffsetCFlag, Cs8 | Cread | Clocal);

                // VMIN=0 / VTIME=0.1秒単位。0 にすると即時復帰でCPUを食い潰すので最低 1 は入れる。
                var vtime = (settings.ReadTimeoutMilliseconds + 99) / 100;
                Marshal.WriteByte(termios, OffsetControlChars + Vmin, 0);
                Marshal.WriteByte(termios, OffsetControlChars + Vtime, (byte)Math.Min(255, Math.Max(1, vtime)));

                var speed = settings.BaudRate == 115200 ? Baud115200 : (ulong)settings.BaudRate;
                if (cfsetispeed(termios, (UIntPtr)speed) != 0 || cfsetospeed(termios, (UIntPtr)speed) != 0)
                {
                    throw new IOException($"{PortName} のボーレートを設定できませんでした");
                }

                if (tcsetattr(_fd, TcsaNow, termios) != 0)
                {
                    throw new IOException($"{PortName} の termios を適用できませんでした (errno {Marshal.GetLastWin32Error()})");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(termios);
            }
        }

        private static void WriteFlag(IntPtr termios, int offset, uint value)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Marshal.WriteInt64(termios, offset, value);
#else
            Marshal.WriteInt32(termios, offset, (int)value);
#endif
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            EnsureOpen();

            var target = offset == 0 ? buffer : new byte[count];

            while (true)
            {
                var read = (int)read_(_fd, target, (IntPtr)count).ToInt64();
                if (read >= 0)
                {
                    if (offset != 0 && read > 0) Buffer.BlockCopy(target, 0, buffer, offset, read);
                    return read;
                }

                // EINTR。シグナルで中断されただけなので読み直す。
                if (Marshal.GetLastWin32Error() == 4) continue;

                throw new IOException($"{PortName} の読み取りに失敗しました (errno {Marshal.GetLastWin32Error()})");
            }
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

            var remaining = count;
            var written = 0;
            while (remaining > 0)
            {
                var chunk = source;
                if (written != 0)
                {
                    chunk = new byte[remaining];
                    Buffer.BlockCopy(source, written, chunk, 0, remaining);
                }

                var result = (int)write_(_fd, chunk, (IntPtr)remaining).ToInt64();
                if (result < 0)
                {
                    if (Marshal.GetLastWin32Error() == 4) continue;
                    throw new IOException($"{PortName} への書き込みに失敗しました (errno {Marshal.GetLastWin32Error()})");
                }

                written += result;
                remaining -= result;
            }
        }

        public void DiscardBuffers()
        {
            if (_fd < 0) return;
            tcflush(_fd, Tciflush);
        }

        /// <summary>
        /// ここでは何もしない。VMIN=0 / VTIME=読み取りタイムアウト の設定により
        /// read(2) は必ず有限時間で戻るため、起こしてやる必要が無い。
        /// この実装はエディタでの動作確認用で、PCVR の実行経路には乗らない。
        /// </summary>
        public void CancelPendingIo()
        {
        }

        private void EnsureOpen()
        {
            if (_fd < 0) throw new IOException($"{PortName} は閉じられています");
        }

        public void Dispose()
        {
            if (_fd < 0) return;

            close(_fd);
            _fd = -1;
        }

        /// <summary>
        /// macOS は callout デバイス (/dev/cu.*) を使う。dial-in 側 (/dev/tty.*) は
        /// キャリア検出を待ってしまうため、こちらの用途には向かない。
        /// </summary>
        public static IReadOnlyList<string> ListPortNames()
        {
            var names = new List<string>();
            try
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                names.AddRange(Directory.GetFiles("/dev", "cu.*"));
#else
                names.AddRange(Directory.GetFiles("/dev", "ttyACM*"));
                names.AddRange(Directory.GetFiles("/dev", "ttyUSB*"));
#endif
            }
            catch (Exception)
            {
                // /dev を読めない環境では咀嚼計なしとして扱う。
                return Array.Empty<string>();
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        // ------------------------------------------------------------------
        // libc
        // ------------------------------------------------------------------

        [DllImport(Libc, SetLastError = true)]
        private static extern int open([MarshalAs(UnmanagedType.LPStr)] string path, int flags);

        [DllImport(Libc, SetLastError = true)]
        private static extern int close(int fd);

        [DllImport(Libc, EntryPoint = "read", SetLastError = true)]
        private static extern IntPtr read_(int fd, byte[] buffer, IntPtr count);

        [DllImport(Libc, EntryPoint = "write", SetLastError = true)]
        private static extern IntPtr write_(int fd, byte[] buffer, IntPtr count);

        [DllImport(Libc, SetLastError = true)]
        private static extern int fcntl(int fd, int cmd, int arg);

        [DllImport(Libc, SetLastError = true)]
        private static extern int tcgetattr(int fd, IntPtr termios);

        [DllImport(Libc, SetLastError = true)]
        private static extern int tcsetattr(int fd, int optionalActions, IntPtr termios);

        [DllImport(Libc, SetLastError = true)]
        private static extern int tcflush(int fd, int queueSelector);

        [DllImport(Libc, SetLastError = true)]
        private static extern int cfsetispeed(IntPtr termios, UIntPtr speed);

        [DllImport(Libc, SetLastError = true)]
        private static extern int cfsetospeed(IntPtr termios, UIntPtr speed);
    }
}
#endif
