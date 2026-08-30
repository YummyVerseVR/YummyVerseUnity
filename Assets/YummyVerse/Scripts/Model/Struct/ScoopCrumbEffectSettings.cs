using System;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// すくったときに飛び散る食べかす (Particle) の見た目のチューニング値。
    /// ScoopDetectionSettings と同じく View に SerializeField で持たせて Inspector から調整する。
    ///
    /// 既定値は食べ物の既定スケール (FoodScaleManager の 0.2 = 実物大相当) を前提にした、
    /// 数 mm 程度のかけらが手元から手前上方へ散る大きさになっている。
    /// </summary>
    [Serializable]
    public class ScoopCrumbEffectSettings
    {
        [Tooltip("1回のすくいで出す食べかすの最小個数")]
        [SerializeField, Min(1)] private int minCount = 8;

        [Tooltip("1回のすくいで出す食べかすの最大個数")]
        [SerializeField, Min(1)] private int maxCount = 16;

        [Tooltip("食べかす1粒が消えるまでの最短時間 (秒)")]
        [SerializeField, Min(0.05f)] private float minLifetimeSeconds = 0.35f;

        [Tooltip("食べかす1粒が消えるまでの最長時間 (秒)")]
        [SerializeField, Min(0.05f)] private float maxLifetimeSeconds = 0.9f;

        [Tooltip("飛び出す速さの下限 (m/s)")]
        [SerializeField, Min(0f)] private float minSpeed = 0.25f;

        [Tooltip("飛び出す速さの上限 (m/s)")]
        [SerializeField, Min(0f)] private float maxSpeed = 0.8f;

        [Tooltip("食べかす1粒の最小の大きさ (m)")]
        [SerializeField, Min(0.0005f)] private float minSize = 0.004f;

        [Tooltip("食べかす1粒の最大の大きさ (m)")]
        [SerializeField, Min(0.0005f)] private float maxSize = 0.012f;

        [Tooltip("重力の効き方。1 で通常の重力どおりに落ちる")]
        [SerializeField, Min(0f)] private float gravityModifier = 0.8f;

        [Tooltip("飛び散る向きの広がり (度)。0 で一直線、大きいほど放射状")]
        [SerializeField, Range(0f, 90f)] private float spreadAngleDegrees = 35f;

        [Tooltip("湧き出す位置のばらつき (m)。接触点を中心とした半径")]
        [SerializeField, Min(0f)] private float spawnRadius = 0.012f;

        [Tooltip("食べかすの色 (濃い側)")]
        [SerializeField] private Color darkColor = new(0.45f, 0.32f, 0.18f, 1f);

        [Tooltip("食べかすの色 (薄い側)。この2色の間でランダムに決まる")]
        [SerializeField] private Color lightColor = new(0.78f, 0.60f, 0.38f, 1f);

        [Tooltip("食べかすに使うマテリアル。未指定なら実行時に生成する (ビルドではここに割り当てるのが確実)")]
        [SerializeField] private Material material;

        public int MinCount => Mathf.Min(minCount, maxCount);
        public int MaxCount => Mathf.Max(minCount, maxCount);
        public float MinLifetimeSeconds => Mathf.Min(minLifetimeSeconds, maxLifetimeSeconds);
        public float MaxLifetimeSeconds => Mathf.Max(minLifetimeSeconds, maxLifetimeSeconds);
        public float MinSpeed => Mathf.Min(minSpeed, maxSpeed);
        public float MaxSpeed => Mathf.Max(minSpeed, maxSpeed);
        public float MinSize => Mathf.Min(minSize, maxSize);
        public float MaxSize => Mathf.Max(minSize, maxSize);
        public float GravityModifier => gravityModifier;
        public float SpreadAngleDegrees => spreadAngleDegrees;
        public float SpawnRadius => spawnRadius;
        public Color DarkColor => darkColor;
        public Color LightColor => lightColor;
        public Material Material => material;
    }
}
