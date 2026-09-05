namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// XR ランタイム(セッション)が通常どおり描画できる状態かどうか。
    ///
    /// PCVR (Quest Link) では HMD の着脱でセッションが STOPPING → IDLE → READY を往復する。
    /// その間はコンポジタが居ないか作り直しの最中で、重い描画を積むと
    /// ランタイム側と噛み合わずにメインスレッドごと止まることがある。
    ///
    /// これは描画負荷の判断にだけ使う値で、来場者の在・不在を表すものではない。
    /// 体験の進行 (開始・中断・リセット) をこの値で動かさないこと。
    /// </summary>
    public enum XrSessionState
    {
        /// <summary>通常どおり描画してよい。</summary>
        Available,

        /// <summary>HMD を外した / フォーカスを失った / 復帰直後の落ち着き待ち。描画負荷を落とす。</summary>
        Suspended
    }
}
