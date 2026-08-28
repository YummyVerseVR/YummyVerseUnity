using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// Owns the Unity-side food runtime representation. FoodView only forwards model
    /// state and lifecycle callbacks; model instantiation, interaction setup, and the
    /// consumption animation live here as one focused presentation collaborator.
    /// </summary>
    public sealed class FoodRuntimePresenter : IDisposable
    {
        private const float ConsumptionShrinkSpeed = 4f;
        private const float DisappearThreshold = 0.02f;

        private readonly IFoodEatingService _eatingService;
        private readonly IScoopProbeProvider _scoopProbeProvider;
        private readonly IScoopHaptics _scoopHaptics;
        private ScoopDetectionSettings _scoopSettings;

        private CancellationTokenSource _displayCancellation;
        private Transform _foodAnchor;
        private GameObject _foodRoot;
        private Transform _currentPlacementTransform;
        private float _baseScale = 1f;
        private float _targetConsumptionScale = 1f;
        private float _currentConsumptionScale = 1f;
        private bool _initialized;
        private bool _disposed;

        public FoodRuntimePresenter(
            IFoodEatingService eatingService,
            IScoopProbeProvider scoopProbeProvider,
            IScoopHaptics scoopHaptics)
        {
            _eatingService = eatingService ?? throw new ArgumentNullException(nameof(eatingService));
            _scoopProbeProvider = scoopProbeProvider ?? throw new ArgumentNullException(nameof(scoopProbeProvider));
            _scoopHaptics = scoopHaptics;
        }

        public void Initialize(ScoopDetectionSettings settings)
        {
            if (_initialized) return;

            _initialized = true;
            _foodAnchor = new GameObject("FoodWorldAnchor").transform;
            _foodAnchor.gameObject.SetActive(false);
            _foodRoot = CreateFoodRoot();
            _scoopSettings = settings ?? new ScoopDetectionSettings();
        }

        public async UniTask DisplayAsync(
            GltfImport gltfImport,
            Transform initialTransform,
            CancellationToken cancellationToken)
        {
            EnsureInitialized();
            CancelDisplay();
            _displayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _displayCancellation.Token;

            ResetFoodState();
            if (gltfImport == null || _disposed) return;

            try
            {
                var instantiator = new GameObjectInstantiator(gltfImport, _foodRoot.transform);
                var instantiated = await gltfImport.InstantiateMainSceneAsync(instantiator, ct);
                ct.ThrowIfCancellationRequested();

                FoodModelVisualCompatibility.Apply(_foodRoot);
                SetFoodTransform(initialTransform);
                ApplyScale();

                // 起動直後の空の GltfImport でも購読が走るため、実体があるときだけ食べられる状態にする。
                if (instantiated && _foodRoot.transform.childCount > 0)
                {
                    SetUpScoopInteraction();
                }
            }
            catch (OperationCanceledException)
            {
                // A newer selected model or the owning GameObject cancelled this display.
            }
            catch (Exception exception)
            {
                _eatingService.AbandonFood();
                Debug.LogWarning($"[Food] モデルの生成に失敗しました: {exception.Message}");
            }
        }

        public void SetFoodTransform(Transform targetTransform)
        {
            EnsureInitialized();
            if (_foodRoot == null || _foodAnchor == null) return;

            if (targetTransform == null)
            {
                _currentPlacementTransform = null;
                _foodAnchor.gameObject.SetActive(false);
                return;
            }

            _currentPlacementTransform = targetTransform;
            _foodAnchor.gameObject.SetActive(true);
            _foodAnchor.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
            _foodRoot.transform.localPosition = Vector3.zero;
            _foodRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        public void SetBaseScale(float scale)
        {
            _baseScale = scale;
            ApplyScale();
        }

        public void SetRemainingFraction(float fraction)
        {
            _targetConsumptionScale = Mathf.Clamp01(fraction);
        }

        public void Tick(float deltaTime)
        {
            if (!_initialized || _disposed) return;

            if (!Mathf.Approximately(_currentConsumptionScale, _targetConsumptionScale))
            {
                _currentConsumptionScale = Mathf.MoveTowards(
                    _currentConsumptionScale,
                    _targetConsumptionScale,
                    ConsumptionShrinkSpeed * deltaTime);
                ApplyScale();
            }

            TryDisappearWhenCleared();
            if (_currentPlacementTransform != null && _foodAnchor != null)
            {
                _foodAnchor.SetPositionAndRotation(
                    _currentPlacementTransform.position,
                    _currentPlacementTransform.rotation);
            }
        }

        public void ResetFoodState()
        {
            if (!_initialized) return;

            if (_foodRoot != null)
            {
                DestroyObject(_foodRoot);
            }

            _foodRoot = CreateFoodRoot();
            _eatingService.AbandonFood();
            _currentConsumptionScale = 1f;
            _targetConsumptionScale = 1f;
            ApplyScale();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelDisplay();
            _eatingService.AbandonFood();
            DestroyObject(_foodRoot);
            DestroyObject(_foodAnchor != null ? _foodAnchor.gameObject : null);
            _foodRoot = null;
            _foodAnchor = null;
        }

        private void SetUpScoopInteraction()
        {
            if (_foodRoot == null) return;

            var target = _foodRoot.AddComponent<FoodScoopTargetView>();
            if (!target.TryInitialize(
                    _eatingService,
                    _scoopProbeProvider,
                    _scoopHaptics,
                    _scoopSettings))
            {
                DestroyObject(target);
                _eatingService.AbandonFood();
                Debug.LogWarning("[Eating] モデルの形状を取得できないため、当たり判定を作れませんでした。この食べ物はすくえません。");
                return;
            }

            _eatingService.BeginFood();
        }

        private void TryDisappearWhenCleared()
        {
            if (_targetConsumptionScale > 0f || _currentConsumptionScale > DisappearThreshold)
            {
                return;
            }

            if (_foodRoot != null && _foodRoot.GetComponent<FoodScoopTargetView>() != null)
            {
                ResetFoodState();
            }
        }

        private void ApplyScale()
        {
            if (_foodRoot != null)
            {
                _foodRoot.transform.localScale =
                    Vector3.one * (_baseScale * _currentConsumptionScale);
            }
        }

        private GameObject CreateFoodRoot()
        {
            var root = new GameObject("FoodRoot");
            root.transform.SetParent(_foodAnchor, false);
            return root;
        }

        private void EnsureInitialized()
        {
            if (!_initialized) throw new InvalidOperationException("FoodRuntimePresenter must be initialized first.");
        }

        private void CancelDisplay()
        {
            if (_displayCancellation == null) return;
            _displayCancellation.Cancel();
            _displayCancellation.Dispose();
            _displayCancellation = null;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
