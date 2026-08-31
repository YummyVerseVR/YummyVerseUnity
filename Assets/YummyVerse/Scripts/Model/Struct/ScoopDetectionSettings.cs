using System;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// すくい判定のチューニング値。Inspector から調整できるよう View 側に SerializeField で持たせる。
    /// </summary>
    [Serializable]
    public class ScoopDetectionSettings
    {
        [Tooltip("手/コントローラーを中心とした、すくい体積の半径 (m)")]
        [SerializeField, Min(0.001f)] private float probeRadius = 0.045f;

        [Tooltip("一度成立した後、この距離まで離れないと再成立しない (m)。接触境界でのばたつきを防ぐ")]
        [SerializeField, Min(0f)] private float releaseMargin = 0.02f;

        [Tooltip("連続したすくいの最短間隔 (秒)。1回のすくい操作が複数回に数えられるのを防ぐ")]
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.5f;

        [Tooltip("すくい成立時のコントローラー振動の長さ (秒)。0 で振動しない")]
        [SerializeField, Min(0f)] private float hapticSeconds = 0.08f;

        [Tooltip("すくい成立時のコントローラー振動の強さ (0〜1)")]
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.6f;

        public float ProbeRadius => probeRadius;
        public float ReleaseMargin => releaseMargin;
        public float CooldownSeconds => cooldownSeconds;
        public float HapticSeconds => hapticSeconds;
        public float HapticAmplitude => hapticAmplitude;
    }
}
