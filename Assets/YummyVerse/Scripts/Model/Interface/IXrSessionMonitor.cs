using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// XR セッションの生死を観測するだけの口。
    ///
    /// PCVR (Quest Link) で HMD を着脱すると OpenXR セッションが作り直される。
    /// その最中に重い描画を積むとランタイムと噛み合わずにフリーズするため、
    /// 描画側が負荷を落とす判断に使う。
    ///
    /// ここは観測に徹する。体験の進行 (セッションの開始・中断・リセット・ステップの歩み) を
    /// この状態で動かしてはいけない。着脱は来場者の在・不在の根拠にならず、
    /// 進行の判断は従来どおり IdleWatcher と入力イベントだけが担う。
    /// </summary>
    public interface IXrSessionMonitor
    {
        ReadOnlyReactiveProperty<XrSessionState> State { get; }
    }
}
