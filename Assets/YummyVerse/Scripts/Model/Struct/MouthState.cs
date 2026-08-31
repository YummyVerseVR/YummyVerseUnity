namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 咀嚼計が通知する開閉イベント (プロトコル仕様書 §11)。
    ///
    /// これは「今の口の状態」ではなく「今この瞬間に開いた/閉じた」という一過性の事実である。
    /// 交互性の検証・欠落回復は仕様上行わないため、OPEN → OPEN のような並びも
    /// そのまま独立したイベントとして扱う。
    /// </summary>
    public enum MouthState
    {
        Open,
        Closed
    }
}
