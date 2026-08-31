namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>咀嚼計 → Unity のメッセージ種別 (プロトコル仕様書 §8)。</summary>
    public enum ChewingSensorMessageKind
    {
        Ready,
        CalibrationAccepted,
        CalibrationDone,
        CalibrationFailed,
        Mouth
    }

    /// <summary>
    /// 咀嚼計から受信した1行を解釈した結果。
    ///
    /// 受信スレッドで作り、スレッドセーフなキュー経由でメインスレッドへ渡す (仕様書 §15.1)。
    /// そのため参照型のフィールドは不変な string だけに留めている。
    /// </summary>
    public readonly struct ChewingSensorMessage
    {
        public ChewingSensorMessageKind Kind { get; }

        /// <summary>キャリブレーション系メッセージの requestId。それ以外では 0。</summary>
        public uint RequestId { get; }

        /// <summary>CAL_FAILED の理由 (仕様書 §12)。それ以外では null。</summary>
        public string FailureReason { get; }

        /// <summary>MOUTH イベントの開閉。それ以外では既定値。</summary>
        public MouthState MouthState { get; }

        private ChewingSensorMessage(
            ChewingSensorMessageKind kind, uint requestId, string failureReason, MouthState mouthState)
        {
            Kind = kind;
            RequestId = requestId;
            FailureReason = failureReason;
            MouthState = mouthState;
        }

        public static ChewingSensorMessage Ready() =>
            new(ChewingSensorMessageKind.Ready, 0u, null, default);

        public static ChewingSensorMessage CalibrationAccepted(uint requestId) =>
            new(ChewingSensorMessageKind.CalibrationAccepted, requestId, null, default);

        public static ChewingSensorMessage CalibrationDone(uint requestId) =>
            new(ChewingSensorMessageKind.CalibrationDone, requestId, null, default);

        public static ChewingSensorMessage CalibrationFailed(uint requestId, string reason) =>
            new(ChewingSensorMessageKind.CalibrationFailed, requestId, reason, default);

        public static ChewingSensorMessage Mouth(MouthState state) =>
            new(ChewingSensorMessageKind.Mouth, 0u, null, state);

        public override string ToString() => Kind switch
        {
            ChewingSensorMessageKind.Ready => "READY",
            ChewingSensorMessageKind.CalibrationAccepted => $"CAL_ACCEPTED,{RequestId}",
            ChewingSensorMessageKind.CalibrationDone => $"CAL_DONE,{RequestId}",
            ChewingSensorMessageKind.CalibrationFailed => $"CAL_FAILED,{RequestId},{FailureReason}",
            ChewingSensorMessageKind.Mouth => $"MOUTH,{MouthState}",
            _ => Kind.ToString()
        };
    }
}
