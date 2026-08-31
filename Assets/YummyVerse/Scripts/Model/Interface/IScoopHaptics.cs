using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// すくい成立をコントローラーの短い振動で知らせる (FR20, SHOULD)。
    /// 非対応デバイスや失敗で、すくい action 自体を失敗させてはならない。
    /// </summary>
    public interface IScoopHaptics
    {
        void PlayScoopPulse(ScoopHand hand, float amplitude, float durationSeconds);
    }
}
