using Oculus.Interaction;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.View
{
    /// <summary>
    /// 設定画面の表示中だけ有効になる、コントローラーで掴める配置用マーカー。
    /// Spatial Anchor 本体とは別オブジェクトにして、ランタイムの姿勢更新と競合させない。
    /// </summary>
    public sealed class FoodPlacementCubeView : MonoBehaviour
    {
        private const float DefaultDistance = 0.7f;
        private const float DefaultVerticalOffset = -0.15f;

        private IFoodPlacementService _foodPlacementService;
        private GameObject _cube;
        private Material _cubeMaterial;
        private bool _configurationVisible;
        private bool _isEditing;

        [Inject]
        public void Construct(IFoodPlacementService foodPlacementService)
        {
            _foodPlacementService = foodPlacementService;
        }

        private void Start()
        {
            CreateCube();
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
        }

        private void LateUpdate()
        {
            if (_cube == null || !_cube.activeSelf) return;
            _foodPlacementService.UpdateDraftPose(new Pose(_cube.transform.position, _cube.transform.rotation));
        }

        private void OnDestroy()
        {
            if (_cube != null)
            {
                Destroy(_cube);
            }
            if (_cubeMaterial != null)
            {
                Destroy(_cubeMaterial);
            }
        }

        private void CreateCube()
        {
            _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cube.name = "Food Placement Cube (Grip to move)";
            _cube.transform.localScale = Vector3.one * 0.1f;
            _cube.SetActive(false);

            var collider = _cube.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var rigidbody = _cube.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var grabbable = _cube.AddComponent<Grabbable>();
            grabbable.InjectOptionalRigidbody(rigidbody);
            grabbable.InjectOptionalThrowWhenUnselected(false);

            var grabInteractable = _cube.AddComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rigidbody);
            grabInteractable.InjectOptionalPointableElement(grabbable);
            grabInteractable.UseClosestPointAsGrabSource = true;

            var renderer = _cube.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                _cubeMaterial = new Material(shader)
                {
                    color = new Color(0.1f, 0.85f, 1f, 0.85f)
                };
                renderer.sharedMaterial = _cubeMaterial;
            }
        }

        private void UpdateVisibility()
        {
            if (_cube == null) return;
            var isVisible = _configurationVisible && _isEditing;
            if (_cube.activeSelf == isVisible) return;

            if (isVisible)
            {
                if (_foodPlacementService.TryGetSuggestedDraftPose(out var pose))
                {
                    _cube.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
                else
                {
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        var cameraTransform = camera.transform;
                        var position = cameraTransform.position
                                       + cameraTransform.forward * DefaultDistance
                                       + cameraTransform.up * DefaultVerticalOffset;
                        var forwardOnFloor = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
                        var rotation = forwardOnFloor.sqrMagnitude > 0.001f
                            ? Quaternion.LookRotation(forwardOnFloor.normalized, Vector3.up)
                            : Quaternion.identity;
                        _cube.transform.SetPositionAndRotation(position, rotation);
                    }
                }

                _foodPlacementService.UpdateDraftPose(
                    new Pose(_cube.transform.position, _cube.transform.rotation));
            }

            _cube.SetActive(isVisible);
        }
    }
}
