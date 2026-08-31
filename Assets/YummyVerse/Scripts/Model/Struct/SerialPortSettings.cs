namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// COMポートを開くときの通信設定 (プロトコル仕様書 §4)。
    ///
    /// 8bit / パリティなし / ストップビット1 / フロー制御なしは仕様で固定されているため
    /// ここでは持たせず、実装側が常にその設定で開く。
    /// </summary>
    public readonly struct SerialPortSettings
    {
        /// <summary>仕様の規定値。USB CDC では実効速度に影響しない実装もあるが、互換のため必ず設定する。</summary>
        public const int DefaultBaudRate = 115200;

        public int BaudRate { get; }

        /// <summary>
        /// 1回の読み取りが最大どれだけブロックするか。
        /// 読み取りが定期的に戻ることで、同じスレッドから送信キューも掃ける。
        /// </summary>
        public int ReadTimeoutMilliseconds { get; }

        public SerialPortSettings(int baudRate, int readTimeoutMilliseconds)
        {
            BaudRate = baudRate <= 0 ? DefaultBaudRate : baudRate;
            ReadTimeoutMilliseconds = readTimeoutMilliseconds <= 0 ? 100 : readTimeoutMilliseconds;
        }
    }
}
