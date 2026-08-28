using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.View
{
    /// <summary>
    /// 表示中の食べ物に対する当たり判定と、すくいの検出 (FR18, FR19, FR20)。
    /// FoodView が食べ物のルートへ実行時に付ける。ルートごと破棄されるので後始末が要らない。
    ///
    /// 当たり判定は透明な BoxCollider (isTrigger) 1つで、レンダラを持たないため描画されない。
    /// ルートのローカル座標で作るので、表示位置・回転・食事による縮小に自動で追従する。
    /// 判定は Collider.ClosestPoint による球と箱の距離計算で行い、Rigidbody やレイヤー設定に依存しない。
    /// </summary>
    public sealed class FoodScoopTargetView : MonoBehaviour
    {
        private IFoodEatingService _eatingService;
        private IScoopProbeProvider _probeProvider;
        private IScoopHaptics _haptics;
        private ScoopDetectionSettings _settings;
        private ScoopContactDetector _detector;
        private BoxCollider _collider;

        /// <summary>
        /// 当たり判定を構築する。形状を取得できないモデルでは false を返し、
        /// 呼び出し側は interaction ready にしてはならない (FR18)。
        /// </summary>
        public bool TryInitialize(
            IFoodEatingService eatingService,
            IScoopProbeProvider probeProvider,
            IScoopHaptics haptics,
            ScoopDetectionSettings settings)
        {
            _eatingService = eatingService;
            _probeProvider = probeProvider;
            _haptics = haptics;
            _settings = settings;
            _detector = new ScoopContactDetector(settings.ReleaseMargin, settings.CooldownSeconds);

            if (!FoodInteractionBoundsCalculator.TryCalculateLocalBounds(transform, out var localBounds))
            {
                return false;
            }

            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.isTrigger = true;
            _collider.center = localBounds.center;
            _collider.size = localBounds.size;
            return true;
        }

        private void Update()
        {
            if (_collider == null || _eatingService == null || _probeProvider == null) return;

            // 完食後・皿が空の間は判定しない。縮小でスケールが 0 に近づく間の誤検出も防ぐ。
            if (!_eatingService.IsInteractable.CurrentValue)
            {
                _collider.enabled = false;
                return;
            }

            _collider.enabled = true;

            var probes = _probeProvider.GetProbes(_settings.ProbeRadius);
            for (var i = 0; i < probes.Count; i++)
            {
                var probe = probes[i];
                var surfaceDistance = Vector3.Distance(_collider.ClosestPoint(probe.Position), probe.Position)
                                      - probe.Radius;

                if (!_detector.TryRegisterContact(probe.Hand, surfaceDistance, Time.unscaledTime)) continue;
                if (!_eatingService.TryScoop()) continue;

                _haptics?.PlayScoopPulse(probe.Hand, _settings.HapticAmplitude, _settings.HapticSeconds);
            }
        }
    }
}
