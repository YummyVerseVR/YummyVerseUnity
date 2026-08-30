using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>Runtime interaction adapter attached to a displayed food root.</summary>
    public sealed class FoodScoopTargetView : MonoBehaviour
    {
        private IFoodEatingService _eatingService;
        private IScoopProbeProvider _probeProvider;
        private IScoopHaptics _haptics;
        private IScoopCrumbEffect _crumbEffect;
        private ScoopDetectionSettings _settings;
        private ScoopContactDetector _detector;
        private BoxCollider _collider;

        public bool TryInitialize(
            IFoodEatingService eatingService,
            IScoopProbeProvider probeProvider,
            IScoopHaptics haptics,
            IScoopCrumbEffect crumbEffect,
            ScoopDetectionSettings settings)
        {
            _eatingService = eatingService;
            _probeProvider = probeProvider;
            _haptics = haptics;
            _crumbEffect = crumbEffect;
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
                var contactPoint = _collider.ClosestPoint(probe.Position);
                var surfaceDistance = Vector3.Distance(contactPoint, probe.Position) - probe.Radius;

                if (!_detector.TryRegisterContact(probe.Hand, surfaceDistance, Time.unscaledTime)) continue;
                if (!_eatingService.TryScoop()) continue;

                _haptics?.PlayScoopPulse(
                    probe.Hand,
                    _settings.HapticAmplitude,
                    _settings.HapticSeconds);

                // すくえた1回だけ、食べ物の表面から手元方向へ食べかすを飛ばす。
                var burst = ScoopCrumbBurst.Resolve(contactPoint, probe.Position, Vector3.up);
                _crumbEffect?.Play(burst.Position, burst.Direction);
            }
        }
    }
}
