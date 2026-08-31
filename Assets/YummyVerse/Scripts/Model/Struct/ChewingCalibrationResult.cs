namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>キャリブレーション要求の決着 (プロトコル仕様書 §9, §13)。</summary>
    public enum ChewingCalibrationStatus
    {
        /// <summary>CAL_DONE を受信した。</summary>
        Succeeded,

        /// <summary>CAL_FAILED を受信した。理由は <see cref="ChewingCalibrationResult.FailureReason"/>。</summary>
        Failed,

        /// <summary>CAL_ACCEPTED / CAL_DONE のいずれかを待ち切れなかった。</summary>
        TimedOut,

        /// <summary>咀嚼計へ繋がっていない、または要求中に切断された。</summary>
        NotConnected
    }

    /// <summary>
    /// キャリブレーション要求の結果。
    ///
    /// 展示中に咀嚼計が不調でもセッション自体は進めたいので、呼び出し側は
    /// <see cref="IsSuccess"/> が false でも中断せず、次の案内へ進める前提で扱う。
    /// </summary>
    public readonly struct ChewingCalibrationResult
    {
        public ChewingCalibrationStatus Status { get; }

        /// <summary>CAL_FAILED の reason。それ以外では null。</summary>
        public string FailureReason { get; }

        public bool IsSuccess => Status == ChewingCalibrationStatus.Succeeded;

        private ChewingCalibrationResult(ChewingCalibrationStatus status, string failureReason)
        {
            Status = status;
            FailureReason = failureReason;
        }

        public static ChewingCalibrationResult Succeeded() =>
            new(ChewingCalibrationStatus.Succeeded, null);

        public static ChewingCalibrationResult Failed(string reason) =>
            new(ChewingCalibrationStatus.Failed, reason);

        public static ChewingCalibrationResult TimedOut() =>
            new(ChewingCalibrationStatus.TimedOut, null);

        public static ChewingCalibrationResult NotConnected() =>
            new(ChewingCalibrationStatus.NotConnected, null);

        public override string ToString() =>
            string.IsNullOrEmpty(FailureReason) ? Status.ToString() : $"{Status}({FailureReason})";
    }
}
