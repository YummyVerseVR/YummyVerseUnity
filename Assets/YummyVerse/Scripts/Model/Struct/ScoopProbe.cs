using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// すくい判定に使う手の左右。ハンドトラッキング時は手、コントローラー使用時はコントローラーを指す。
    /// haptic の送り先を決めるためだけに保持しており、判定そのものには影響しない。
    /// </summary>
    public enum ScoopHand
    {
        Left,
        Right,
        Other // デバッグ用のダミープローブなど、実機の手に紐付かないもの
    }

    /// <summary>
    /// 食べ物へ接触しうる「すくい体積」を球で近似したもの。
    /// FR19 の spoon interaction volume にあたる。
    /// </summary>
    public readonly struct ScoopProbe
    {
        public ScoopHand Hand { get; }
        public Vector3 Position { get; }
        public float Radius { get; }

        public ScoopProbe(ScoopHand hand, Vector3 position, float radius)
        {
            Hand = hand;
            Position = position;
            Radius = radius;
        }
    }
}
