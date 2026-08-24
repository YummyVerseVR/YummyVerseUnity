using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using Oculus.Interaction;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.View
{
    /// <summary>
    /// 設定画面で食べ物の位置・回転・スケールを確認するための配置プレビュー。
    /// 以前の水色Cubeの代わりに、S5で表示するものと同じローカル食品モデルを使う。
    /// </summary>
    public sealed class FoodPlacementCubeView : MonoBehaviour
    {
        private const float DefaultDistance = 0.7f;
        private const float DefaultVerticalOffset = -0.15f;
        private const float MinimumColliderSize = 0.05f;

        private IFoodPlacementService _foodPlacementService;
        private IFoodScaleManager _foodScaleManager;
        private ILocalFoodSelectionProvider _localFoodSelectionProvider;
        private GameObject _placementMarker;
        private Transform _modelRoot;
        private BoxCollider _interactionCollider;
        private GltfImport _previewGltf;
        private bool _configurationVisible;
        private bool _isEditing;

        [Inject]
        public void Construct(
            IFoodPlacementService foodPlacementService,
            IFoodScaleManager foodScaleManager,
            ILocalFoodSelectionProvider localFoodSelectionProvider)
        {
            _foodPlacementService = foodPlacementService;
            _foodScaleManager = foodScaleManager;
            _localFoodSelectionProvider = localFoodSelectionProvider;
        }

        private void Start()
        {
            CreatePlacementMarker();
            _foodScaleManager.FoodScale
                .Subscribe(SetPreviewScale)
                .AddTo(this);
            _foodPlacementService.IsConfigurationVisible
                .Subscribe(isVisible =>
                {
                    _configurationVisible = isVisible;
                    UpdateVisibility();
                })
                .AddTo(this);
            _foodPlacementService.State
                .Subscribe(state =>
                {
                    _isEditing = state == FoodPlacementState.Editing
                                 || state == FoodPlacementState.Error;
                    UpdateVisibility();
                })
                .AddTo(this);

            LoadPreviewModelAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnEnable()
        {
            // 親の無効化などで一度非表示にした後に再び有効化された場合も、
            // 現在の設定画面状態から表示を再計算する。
            if (_foodPlacementService != null) UpdateVisibility();
        }

        private void OnDisable()
        {
            // 配置用モデルはこのViewのGameObject外に生成しているため、Viewだけが
            // 無効化された場合にも明示的に隠す。
            if (_placementMarker != null) _placementMarker.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_placementMarker == null || !_placementMarker.activeSelf) return;
            _foodPlacementService.UpdateDraftPose(
                new Pose(_placementMarker.transform.position, _placementMarker.transform.rotation));
        }

        private void OnDestroy()
        {
            if (_placementMarker != null)
            {
                if (Application.isPlaying) Destroy(_placementMarker);
                else DestroyImmediate(_placementMarker);
            }
            _previewGltf?.Dispose();
            _previewGltf = null;
        }

        private void CreatePlacementMarker()
        {
            _placementMarker = new GameObject("Food Placement Model (Grip to move)");
            _modelRoot = new GameObject("Food Placement Model Content").transform;
            _modelRoot.SetParent(_placementMarker.transform, false);
            _modelRoot.localPosition = Vector3.zero;
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

        private async UniTaskVoid LoadPreviewModelAsync(CancellationToken ct)
        {
            if (!_localFoodSelectionProvider.TryGetSelected(out var selected))
            {
                Debug.LogWarning(
                    $"[FoodPlacement] プレビューに使えるローカル食品がありません: " +
                    $"{Application.persistentDataPath}/Foods/*/model.glb");
                return;
            }

            GltfImport gltf = null;
            try
            {
                gltf = GltfImportFactory.Create();
                if (!await gltf.Load(selected.ModelLocation, cancellationToken: ct))
                {
                    Debug.LogWarning($"[FoodPlacement] 配置プレビューモデルを読み込めませんでした: {selected.DisplayName}");
                    gltf.Dispose();
                    return;
                }

                ct.ThrowIfCancellationRequested();
                _previewGltf = gltf;
                var instantiator = new GameObjectInstantiator(gltf, _modelRoot);
                await gltf.InstantiateMainSceneAsync(instantiator, ct);
                FoodModelVisualCompatibility.Apply(_placementMarker);
                RefreshInteractionCollider();
                Debug.Log($"[FoodPlacement] 配置プレビューを読み込みました: {selected.DisplayName}");
            }
            catch (OperationCanceledException)
            {
                // GameObject破棄時のキャンセルは正常終了。
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
            RefreshInteractionCollider();
        }

        private void RefreshInteractionCollider()
        {
            if (_interactionCollider == null || _placementMarker == null) return;

            var renderers = _placementMarker.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            var markerTransform = _placementMarker.transform;
            var localBounds = new Bounds(
                markerTransform.InverseTransformPoint(renderers[0].bounds.center),
                Vector3.zero);
            foreach (var renderer in renderers)
            {
                EncapsulateWorldBounds(ref localBounds, markerTransform, renderer.bounds);
            }

            _interactionCollider.center = localBounds.center;
            _interactionCollider.size = new Vector3(
                Mathf.Max(localBounds.size.x, MinimumColliderSize),
                Mathf.Max(localBounds.size.y, MinimumColliderSize),
                Mathf.Max(localBounds.size.z, MinimumColliderSize));
        }

        private static void EncapsulateWorldBounds(
            ref Bounds target,
            Transform localSpace,
            Bounds worldBounds)
        {
            var min = worldBounds.min;
            var max = worldBounds.max;
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var z = 0; z < 2; z++)
                    {
                        target.Encapsulate(localSpace.InverseTransformPoint(new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z)));
                    }
                }
            }
        }

        private void UpdateVisibility()
        {
            if (_placementMarker == null) return;
            var isVisible = _configurationVisible && _isEditing;
            var wasVisible = _placementMarker.activeSelf;

            if (isVisible && !wasVisible)
            {
                if (_foodPlacementService.TryGetSuggestedDraftPose(out var pose))
                {
                    _placementMarker.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
                else
                {
                    PositionInFrontOfCamera();
                }

                _foodPlacementService.UpdateDraftPose(
                    new Pose(_placementMarker.transform.position, _placementMarker.transform.rotation));
            }

            _placementMarker.SetActive(isVisible);
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
    }
}
