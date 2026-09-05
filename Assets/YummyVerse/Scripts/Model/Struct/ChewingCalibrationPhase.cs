namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// キャリブレーションの測定フェーズ (プロトコル仕様書 §9)。
    ///
    /// 測定は必ず Noise → Chew の順で行う。ノイズ測定は「静止」ではなく
    /// 「咀嚼ではないが口元は動いている」状態を測るためのもので、この2つの実測値の
    /// 間に閾値が決まる。順序を入れ替えると咀嚼計は NOT_STARTED を返す。
    /// </summary>
    public enum ChewingCalibrationPhase
    {
        /// <summary>小さく歯をカチカチさせた状態。無視すべき動きの上限を測る。</summary>
        Noise,

        /// <summary>奥歯で噛みしめた状態。検知すべき動きの大きさを測る。</summary>
        Chew
    }
}
