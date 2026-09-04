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
        private FoodRevealSettings _revealSettings;
        private ScoopCrumbEffectController _crumbEffect;
        private FoodDomeController _dome;
        private FoodRevealSmokeEffect _revealSmoke;

        private CancellationTokenSource _displayCancellation;
        private Transform _foodAnchor;
        private GameObject _foodRoot;
        private Transform _currentPlacementTransform;
        private float _baseScale = 1f;
        private float _targetConsumptionScale = 1f;
        private float _currentConsumptionScale = 1f;
        private bool _initialized;
        private bool _disposed;

        /// <summary>煙を出し始めた時刻 (Time.time)。出していない間は負。</summary>
        private float _revealStartedAt = -1f;

        public FoodRuntimePresenter(
            IFoodEatingService eatingService,
            IScoopProbeProvider scoopProbeProvider,
            IScoopHaptics scoopHaptics)
        {
            _eatingService = eatingService ?? throw new ArgumentNullException(nameof(eatingService));
            _scoopProbeProvider = scoopProbeProvider ?? throw new ArgumentNullException(nameof(scoopProbeProvider));
            _scoopHaptics = scoopHaptics;
        }

        public void Initialize(
            ScoopDetectionSettings settings,
            ScoopCrumbEffectSettings crumbEffectSettings,
            FoodRevealSettings revealSettings)
        {
            if (_initialized) return;

            _initialized = true;
            _foodAnchor = new GameObject("FoodWorldAnchor").transform;
            _foodAnchor.gameObject.SetActive(false);
            _foodRoot = CreateFoodRoot();
            _scoopSettings = settings ?? new ScoopDetectionSettings();
            _revealSettings = revealSettings ?? new FoodRevealSettings();

            // 食べ物の縮小・破棄に粒が巻き込まれないよう、食べかすは食べ物の階層の外で持つ。
            _crumbEffect = new ScoopCrumbEffectController(crumbEffectSettings);

            // ドームは食べ物と同じ anchor の子。皿の追従は anchor 側がまとめて面倒を見る。
            _dome = new FoodDomeController(_revealSettings, _foodAnchor);
            _revealSmoke = new FoodRevealSmokeEffect(_revealSettings);
        }

        /// <summary>
        /// 食べ物の準備中かどうかを受け取り、フードドームの出し入れと現れる瞬間の煙を担う。
        /// 準備が終わってから食べ物が届くまでの間は、煙だけが出ている状態になる。
        /// </summary>
        public void SetPreparing(bool preparing)
        {
            if (!_initialized || _disposed) return;

            if (preparing)
            {
                // 前の食べ物がドームの中に取り残されないよう、置き場所を空にしてから被せる。
                ResetFoodState();
                _revealStartedAt = -1f;
                _dome.SetVisible(true);
                SyncDomePose();
                return;
            }

            if (!_dome.IsVisible) return;

            _dome.SetVisible(false);

            // 置き場所が決まっていなければドームも見えていない。あらぬ所で煙を出さない。
            if (_foodAnchor == null || !_foodAnchor.gameObject.activeSelf) return;

            _revealSmoke.Play(RevealPosition());
            _revealStartedAt = Time.time;
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
                // ドームが消えた直後なら、煙を出し切ってから食べ物を出す。
                await WaitForRevealSmokeAsync(ct);

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

            SyncDomePose();
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
            _crumbEffect?.Dispose();
            _crumbEffect = null;
            _dome?.Dispose();
            _dome = null;
            _revealSmoke?.Dispose();
            _revealSmoke = null;
            DestroyObject(_foodRoot);
            DestroyObject(_foodAnchor != null ? _foodAnchor.gameObject : null);
            _foodRoot = null;
            _foodAnchor = null;
        }

        private async UniTask WaitForRevealSmokeAsync(CancellationToken cancellationToken)
        {
            if (_revealStartedAt < 0f) return;

            var remaining = _revealSettings.SmokeDurationSeconds - (Time.time - _revealStartedAt);
            _revealStartedAt = -1f;
            if (remaining <= 0f) return;

            await UniTask.Delay(
                TimeSpan.FromSeconds(remaining),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// ドームの位置だけを食べ物へ合わせ直す。回転は FoodDomeController がワールド無回転に固定するため、
        /// 皿の傾きに関係なく取っ手は常に上を向く。
        /// </summary>
        private void SyncDomePose()
        {
            if (_dome == null || !_dome.IsVisible || _foodAnchor == null) return;
            _dome.SyncPose(_foodAnchor.position);
        }

        private Vector3 RevealPosition()
        {
            if (_foodAnchor == null) return Vector3.zero;
            return _foodAnchor.position + Vector3.up * _revealSettings.SmokeHeightOffset;
        }

        private void SetUpScoopInteraction()
        {
            if (_foodRoot == null) return;

            var target = _foodRoot.AddComponent<FoodScoopTargetView>();
            if (!target.TryInitialize(
                    _eatingService,
                    _scoopProbeProvider,
                    _scoopHaptics,
                    _crumbEffect,
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
