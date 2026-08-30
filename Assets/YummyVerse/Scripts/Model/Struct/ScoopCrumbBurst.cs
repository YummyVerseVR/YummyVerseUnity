using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 1回のすくいで食べかすを噴き出す位置と向き。
    /// すくい体積 (手/スプーン) と食べ物の当たり判定の接触点から決まる、表示技術に依らない値。
    /// </summary>
    public readonly struct ScoopCrumbBurst
    {
        /// <summary>食べ物の表面上の湧き出し位置 (world)。</summary>
        public Vector3 Position { get; }

        /// <summary>食べかすが飛び散る中心方向 (正規化済み)。</summary>
        public Vector3 Direction { get; }

        public ScoopCrumbBurst(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        /// <summary>接触点から手元へ向かう向きを軸に、少し上向きへ持ち上げる割合。</summary>
        private const float UpwardBias = 0.5f;

        private const float Epsilon = 1e-6f;

        /// <summary>
        /// 接触点とすくい体積の中心から、食べかすの噴出しを決める。
        /// </summary>
        /// <param name="contactPoint">食べ物の当たり判定上の、すくい体積に最も近い点 (world)</param>
        /// <param name="probePosition">すくい体積の中心 (world)</param>
        /// <param name="fallbackDirection">
        /// 接触点と中心が一致する (めり込んでいる) ときに使う向き。通常は world の上方向。
        /// </param>
        public static ScoopCrumbBurst Resolve(
            Vector3 contactPoint,
            Vector3 probePosition,
            Vector3 fallbackDirection)
        {
            var fallback = fallbackDirection.sqrMagnitude > Epsilon
                ? fallbackDirection.normalized
                : Vector3.up;

            // めり込んでいる間 ClosestPoint は中心そのものを返すため、外向きが取れない。
            var outward = probePosition - contactPoint;
            if (outward.sqrMagnitude <= Epsilon) return new ScoopCrumbBurst(contactPoint, fallback);

            // 真横に散ると皿へ落ちるだけなので、手元方向を少し上へ持ち上げて弧を描かせる。
            var lifted = outward.normalized + fallback * UpwardBias;
            var direction = lifted.sqrMagnitude <= Epsilon ? fallback : lifted.normalized;
            return new ScoopCrumbBurst(contactPoint, direction);
        }
    }
}
