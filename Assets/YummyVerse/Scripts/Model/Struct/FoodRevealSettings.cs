using System;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 食べ物が出てくるまでの「準備中」演出のチューニング値。
    /// ScoopCrumbEffectSettings と同じく View に SerializeField で持たせて Inspector から調整する。
    ///
    /// 演出の流れ:
    ///   選択画面に入る  → 食べ物の位置にフードドームを置く
    ///   ダウンロード完了 → ドームを消し、白い煙を SmokeDurationSeconds だけ出す
    ///   煙の再生が終わる → 選ばれた食べ物を出す
    /// </summary>
    [Serializable]
    public class FoodRevealSettings
    {
        [Tooltip("ダウンロード中に食べ物の位置へ置くフードドームのモデル。"
                 + "Assets/YummyVerse/Prefabs/Restaurant/FoodDoom.glb を割り当てる")]
        [SerializeField] private GameObject domePrefab;

        [Tooltip("フードドームの大きさ倍率。モデルの実寸に対する倍率で、食べ物のスケール設定とは独立")]
        [SerializeField, Min(0.001f)] private float domeScale = 1f;

        [Tooltip("フードドームを置く高さ (m)。食べ物の位置からのワールド上方向オフセット。"
                 + "ドームの姿勢は皿の傾きによらず常に無回転 (取っ手が上) で固定される")]
        [SerializeField] private float domeHeightOffset;

        [Tooltip("ドームを消してから食べ物を出すまでの、白い煙の再生時間 (秒)")]
        [SerializeField, Min(0.01f)] private float smokeDurationSeconds = 0.5f;

        [Tooltip("煙を出す高さ (m)。食べ物の位置からのワールド上方向オフセット")]
        [SerializeField] private float smokeHeightOffset = 0.05f;

        [Tooltip("1回の演出で出す煙の最小個数")]
        [SerializeField, Min(1)] private int minCount = 22;

        [Tooltip("1回の演出で出す煙の最大個数")]
        [SerializeField, Min(1)] private int maxCount = 32;

        [Tooltip("煙1つが消えるまでの最短時間 (秒)")]
        [SerializeField, Min(0.05f)] private float minLifetimeSeconds = 0.35f;

        [Tooltip("煙1つが消えるまでの最長時間 (秒)")]
        [SerializeField, Min(0.05f)] private float maxLifetimeSeconds = 0.7f;

        [Tooltip("湧き出す速さの下限 (m/s)")]
        [SerializeField, Min(0f)] private float minSpeed = 0.25f;

        [Tooltip("湧き出す速さの上限 (m/s)")]
        [SerializeField, Min(0f)] private float maxSpeed = 0.7f;

        [Tooltip("煙1つの最小の大きさ (m)")]
        [SerializeField, Min(0.001f)] private float minSize = 0.06f;

        [Tooltip("煙1つの最大の大きさ (m)")]
        [SerializeField, Min(0.001f)] private float maxSize = 0.16f;

        [Tooltip("重力の効き方。負の値でゆっくり立ちのぼる")]
        [SerializeField] private float gravityModifier = -0.08f;

        [Tooltip("真上からの広がり (度)。大きいほど横へ広がりながら立ちのぼる")]
        [SerializeField, Range(0f, 90f)] private float spreadAngleDegrees = 55f;

        [Tooltip("湧き出す位置のばらつき (m)。食べ物の中心を基準とした半径")]
        [SerializeField, Min(0f)] private float spawnRadius = 0.07f;

        [Tooltip("煙の色。白い煙なので既定は半透明の白")]
        [SerializeField] private Color smokeColor = new(1f, 1f, 1f, 0.85f);

        [Tooltip("煙に使うマテリアル。未指定なら実行時に生成する (ビルドではここに割り当てるのが確実)")]
        [SerializeField] private Material material;

        public GameObject DomePrefab => domePrefab;
        public float DomeScale => domeScale;
        public float DomeHeightOffset => domeHeightOffset;
        public float SmokeDurationSeconds => smokeDurationSeconds;
        public float SmokeHeightOffset => smokeHeightOffset;
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
        public Color SmokeColor => smokeColor;
        public Material Material => material;
    }
}
