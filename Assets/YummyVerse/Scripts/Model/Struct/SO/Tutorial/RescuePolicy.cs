namespace YummyVerse.Scripts.Model.Struct.SO.Tutorial
{
    /// <summary>
    /// TaskStep が rescueTimeoutSeconds まで達成されなかったときの扱い。
    /// 既定は AutoAdvance。来場者を絶対に立ち往生させないこと。
    /// </summary>
    public enum RescuePolicy
    {
        AutoAdvance,     // 達成扱いにせず次へ進む
        ForceComplete,   // ゲーム側に完了を代行させる(GameCommand を送る)
        ReturnToAttract  // セッションを中断する
    }

    /// <summary>
    /// TaskStep がどの段階まで進んだか。アナリティクス記録用。
    /// </summary>
    public enum TutorialStepPhase
    {
        Entered,
        HintShown,
        Succeeded,
        Rescued
    }
}
