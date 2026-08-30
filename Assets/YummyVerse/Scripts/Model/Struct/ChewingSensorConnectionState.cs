namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 咀嚼計との接続状態 (プロトコル仕様書 §16 の Unity 側状態)。
    /// </summary>
    public enum ChewingSensorConnectionState
    {
        /// <summary>未接続。まだ探索も始まっていない、または切断されて次の探索を待っている。</summary>
        Disconnected,

        /// <summary>COMポートを1つずつ開いて HELLO / READY ハンドシェイクを試している。</summary>
        Discovering,

        /// <summary>適合デバイスを採用済み。開閉イベントとキャリブレーションを扱える。</summary>
        Connected
    }
}
