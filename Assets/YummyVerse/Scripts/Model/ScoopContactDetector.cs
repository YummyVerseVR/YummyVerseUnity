using System.Collections.Generic;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// 「すくい体積と食べ物の当たり判定が接触している」という毎フレームの事実を、
    /// 1回のすくい action へ正規化する純粋な状態機械 (FR19)。
    ///
    /// - 接触したフレームで一度だけ成立する。接触し続けている間は再成立しない。
    /// - 一度離れる (releaseMargin まで離れる) までは再成立しない。境界でのばたつき対策。
    /// - 成立から cooldown 未満の再接触は、1回のすくい操作の揺り戻しとみなして数えない。
    ///
    /// 表示技術にも Unity の物理にも依存しないため、EditMode テストで検証できる。
    /// </summary>
    public sealed class ScoopContactDetector
    {
        private readonly float _releaseMargin;
        private readonly float _cooldownSeconds;
        private readonly Dictionary<ScoopHand, ProbeState> _states = new();

        public ScoopContactDetector(float releaseMargin, float cooldownSeconds)
        {
            _releaseMargin = releaseMargin < 0f ? 0f : releaseMargin;
            _cooldownSeconds = cooldownSeconds < 0f ? 0f : cooldownSeconds;
        }

        /// <summary>
        /// 1フレーム分の接触状況を渡し、新しいすくいが成立したかを返す。
        /// </summary>
        /// <param name="hand">左右どちらのすくい体積か</param>
        /// <param name="surfaceDistance">
        /// すくい体積の表面から食べ物の当たり判定までの距離 (m)。めり込んでいる間は 0 以下。
        /// </param>
        /// <param name="time">単調増加する時刻 (秒)</param>
        public bool TryRegisterContact(ScoopHand hand, float surfaceDistance, float time)
        {
            if (!_states.TryGetValue(hand, out var state))
            {
                state = new ProbeState { LastScoopTime = float.NegativeInfinity };
            }

            var scooped = false;

            if (state.Engaged)
            {
                // 十分離れるまでは接触継続とみなし、次のすくいを成立させない。
                if (surfaceDistance > _releaseMargin) state.Engaged = false;
            }
            else if (surfaceDistance <= 0f)
            {
                state.Engaged = true;
                if (time - state.LastScoopTime >= _cooldownSeconds)
                {
                    state.LastScoopTime = time;
                    scooped = true;
                }
            }

            _states[hand] = state;
            return scooped;
        }

        /// <summary>
        /// 食べ物が入れ替わった / セッションが切り替わったときに接触状態を捨てる。
        /// </summary>
        public void Reset() => _states.Clear();

        private struct ProbeState
        {
            public bool Engaged;
            public float LastScoopTime;
        }
    }
}
