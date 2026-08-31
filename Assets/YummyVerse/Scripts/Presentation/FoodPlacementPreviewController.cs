using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using Oculus.Interaction;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Infrastructure;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// Owns the placement preview GameObject and its interaction adapter. The Unity View
    /// delegates placement policy, model loading, collider sizing, and visibility here.
    /// </summary>
    public sealed class FoodPlacementPreviewController : IDisposable
    {
        private const float DefaultDistance = 0.7f;
        private const float DefaultVerticalOffset = -0.15f;

        private readonly IFoodPlacementService _placementService;
        private readonly IFoodScaleManager _scaleManager;
        private readonly ILocalFoodSelectionProvider _localFoodSelectionProvider;
        private readonly CompositeDisposable _disposables = new();

        private GameObject _placementMarker;
        private Transform _modelRoot;
        private BoxCollider _interactionCollider;
        private GltfImport _previewGltf;
        private bool _configurationVisible;
        private bool _isEditing;
        private bool _hostEnabled;
        private bool _initialized;
        private bool _disposed;

        public FoodPlacementPreviewController(
            IFoodPlacementService placementService,
            IFoodScaleManager scaleManager,
            ILocalFoodSelectionProvider localFoodSelectionProvider)
        {
            _placementService = placementService ?? throw new ArgumentNullException(nameof(placementService));
            _scaleManager = scaleManager ?? throw new ArgumentNullException(nameof(scaleManager));
            _localFoodSelectionProvider = localFoodSelectionProvider
                                          ?? throw new ArgumentNullException(nameof(localFoodSelectionProvider));
        }

        public void Initialize(Transform owner, CancellationToken cancellationToken)
        {
            if (_initialized || _disposed) return;
            _initialized = true;
            _hostEnabled = owner != null && owner.gameObject.activeInHierarchy;
            CreatePlacementMarker();

            _scaleManager.FoodScale.Subscribe(SetPreviewScale).AddTo(_disposables);
            _placementService.IsConfigurationVisible
                .Subscribe(SetConfigurationVisible)
                .AddTo(_disposables);
            _placementService.State
                .Subscribe(SetPlacementState)
                .AddTo(_disposables);

            LoadPreviewModelAsync(cancellationToken).Forget();
        }

        public void SetHostEnabled(bool enabled)
        {
            _hostEnabled = enabled;
            UpdateVisibility();
        }

        public void Tick()
        {
            if (_placementMarker == null || !_placementMarker.activeSelf || _disposed) return;
            _placementService.UpdateDraftPose(new Pose(
                _placementMarker.transform.position,
                _placementMarker.transform.rotation));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _disposables.Dispose();
            if (_placementMarker != null)
            {
                DestroyObject(_placementMarker);
            }

            _previewGltf?.Dispose();
            _previewGltf = null;
            _placementMarker = null;
        }

        private void SetConfigurationVisible(bool isVisible)
        {
            _configurationVisible = isVisible;
            UpdateVisibility();
        }

        private void SetPlacementState(FoodPlacementState state)
        {
            _isEditing = state == FoodPlacementState.Editing
                          || state == FoodPlacementState.Error;
            UpdateVisibility();
        }

        private void CreatePlacementMarker()
        {
            _placementMarker = new GameObject("Food Placement Model (Grip to move)");
            _modelRoot = new GameObject("Food Placement Model Content").transform;
            _modelRoot.SetParent(_placementMarker.transform, false);
            _modelRoot.localRotation = Quaternion.Euler(90f, 0f, 0f);

            _interactionCollider = _placementMarker.AddComponent<BoxCollider>();
            _interactionCollider.isTrigger = true;
            _interactionCollider.size = Vector3.one * 0.12f;

            var rigidbody = _placementMarker.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var grabbable = _placementMarker.AddComponent<Grabbable>();
            grabbable.InjectOptionalRigidbody(rigidbody);
            grabbable.InjectOptionalThrowWhenUnselected(false);

            var grabInteractable = _placementMarker.AddComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rigidbody);
            grabInteractable.InjectOptionalPointableElement(grabbable);
            grabInteractable.UseClosestPointAsGrabSource = true;

            var distanceGrabInteractable = _placementMarker.AddComponent<DistanceGrabInteractable>();
            distanceGrabInteractable.InjectRigidbody(rigidbody);
            distanceGrabInteractable.InjectOptionalGrabSource(_placementMarker.transform);
            distanceGrabInteractable.InjectOptionalPointableElement(grabbable);

            _placementMarker.SetActive(false);
        }

        private async UniTaskVoid LoadPreviewModelAsync(CancellationToken cancellationToken)
        {
            if (!_localFoodSelectionProvider.TryGetSelected(out var selected))
            {
                Debug.LogWarning("[FoodPlacement] 配置プレビューに使えるローカル食品がありません。");
                return;
            }

            GltfImport gltf = null;
            try
            {
                gltf = GltfImportFactory.Create();
                if (!await gltf.Load(selected.ModelLocation, cancellationToken: cancellationToken))
                {
                    Debug.LogWarning($"[FoodPlacement] 配置プレビューモデルを読み込めませんでした: {selected.DisplayName}");
                    gltf.Dispose();
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                _previewGltf = gltf;
                var instantiator = new GameObjectInstantiator(gltf, _modelRoot);
                await gltf.InstantiateMainSceneAsync(instantiator, cancellationToken);
                FoodModelVisualCompatibility.Apply(_placementMarker);
                FoodPlacementColliderCalculator.TryApply(_interactionCollider, _placementMarker.transform);
                Debug.Log($"[FoodPlacement] 配置プレビューを読み込みました: {selected.DisplayName}");
            }
            catch (OperationCanceledException)
            {
                if (!ReferenceEquals(gltf, _previewGltf)) gltf?.Dispose();
            }
            catch (Exception exception)
            {
                if (!ReferenceEquals(gltf, _previewGltf)) gltf?.Dispose();
                Debug.LogWarning($"[FoodPlacement] 配置プレビューの生成に失敗しました: {exception.Message}");
            }
        }

        private void SetPreviewScale(float scale)
        {
            if (_modelRoot == null) return;
            _modelRoot.localScale = Vector3.one * scale;
            FoodPlacementColliderCalculator.TryApply(_interactionCollider, _placementMarker.transform);
        }

        private void UpdateVisibility()
        {
            if (_placementMarker == null) return;
            var visible = _hostEnabled && _configurationVisible && _isEditing;
            var wasVisible = _placementMarker.activeSelf;

            if (visible && !wasVisible)
            {
                if (_placementService.TryGetSuggestedDraftPose(out var pose))
                {
                    _placementMarker.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
                else
                {
                    PositionInFrontOfCamera();
                }

                _placementService.UpdateDraftPose(new Pose(
                    _placementMarker.transform.position,
                    _placementMarker.transform.rotation));
            }

            _placementMarker.SetActive(visible);
        }

        private void PositionInFrontOfCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;

            var cameraTransform = camera.transform;
            var position = cameraTransform.position
                           + cameraTransform.forward * DefaultDistance
                           + cameraTransform.up * DefaultVerticalOffset;
            var forwardOnFloor = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            var rotation = forwardOnFloor.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(forwardOnFloor.normalized, Vector3.up)
                : Quaternion.identity;
            _placementMarker.transform.SetPositionAndRotation(position, rotation);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
