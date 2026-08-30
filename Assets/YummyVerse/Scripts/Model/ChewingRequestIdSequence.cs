namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// キャリブレーション要求IDの発番 (プロトコル仕様書 §10)。
    ///
    /// 予約値の 0 は使わず、uint.MaxValue の次は 1 へ折り返す。
    /// 咀嚼計との照合は一致判定だけで行うため、大小比較で新旧を決めてはならない。
    /// </summary>
    public sealed class ChewingRequestIdSequence
    {
        private uint _current = ChewingSensorProtocol.NoRequestId;

        public uint Current => _current;

        public uint Next()
        {
            _current = _current == uint.MaxValue ? 1u : _current + 1u;
            return _current;
        }
    }
}
