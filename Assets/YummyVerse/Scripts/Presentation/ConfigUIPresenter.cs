using System;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YummyVerse.Scripts.ViewModel.Interface;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// Binds configuration controls to the configuration use-case port and owns the
    /// world-space presentation details. ConfigUIView is intentionally only the Unity
    /// lifecycle/serialized-reference adapter.
    /// </summary>
    public sealed class ConfigUIPresenter : IDisposable
    {
        private const float FadeDuration = 0.1f;
        private const float DisplayDistance = 0.6f;

        private readonly IConfigUIViewModel _viewModel;
        private readonly CompositeDisposable _disposables = new();
        private Tween _visibilityTween;
        private PointableCanvasInteractionGate _interactionGate;
        private CanvasGroup _canvasGroup;
        private TMP_InputField _apiEndPointUrl;
        private Button _testConnectionButton;
        private OVROverlayCanvas _overlayCanvas;
        private Slider _foodScaleSlider;
        private Camera _targetCamera;
        private Transform _uiTransform;
        private Button _spatialAnchorButton;
        private Button _fixFoodPositionButton;
        private TextMeshProUGUI _spatialPlacementStatus;
        private Button _returnToStartButton;
        private bool _initialized;

        public ConfigUIPresenter(IConfigUIViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void Initialize(
            TMP_InputField apiEndPointUrl,
            Button testConnectionButton,
            CanvasGroup canvasGroup,
            OVROverlayCanvas overlayCanvas,
            Slider foodScaleSlider,
            Camera targetCamera,
            Transform uiTransform,
            Button spatialAnchorButton,
            Button fixFoodPositionButton,
            TextMeshProUGUI spatialPlacementStatus,
            Button returnToStartButton)
        {
            if (_initialized) return;
            _initialized = true;

            _apiEndPointUrl = apiEndPointUrl;
            _testConnectionButton = testConnectionButton;
            _canvasGroup = canvasGroup;
            _overlayCanvas = overlayCanvas;
            _foodScaleSlider = foodScaleSlider;
            _targetCamera = targetCamera;
            _uiTransform = uiTransform;
            _spatialAnchorButton = spatialAnchorButton;
            _fixFoodPositionButton = fixFoodPositionButton;
            _spatialPlacementStatus = spatialPlacementStatus;
            _returnToStartButton = returnToStartButton;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                _interactionGate = new PointableCanvasInteractionGate(_canvasGroup);
            }

            DisableOverlayCanvas();
            BindState();
            BindControls();
        }

        public void OnHostDisabled()
        {
            _viewModel.SetVisible(false);
            Hide();
        }

        public void Dispose()
        {
            _visibilityTween?.Kill();
            DisableOverlayCanvas();
            _disposables.Dispose();
        }

        private void BindState()
        {
            _viewModel.IsVisible
                .Subscribe(SetVisible)
                .AddTo(_disposables);
            _viewModel.APIEndPointUrl
                .Subscribe(value => _apiEndPointUrl?.SetTextWithoutNotify(value))
                .AddTo(_disposables);
            _viewModel.FoodScale
                .Subscribe(value => _foodScaleSlider?.SetValueWithoutNotify(value))
                .AddTo(_disposables);
            _viewModel.SpatialPlacementStatus
                .Subscribe(value =>
                {
                    if (_spatialPlacementStatus != null) _spatialPlacementStatus.text = value;
                })
                .AddTo(_disposables);

            Observable.CombineLatest(
                    _viewModel.IsSpatialPlacementBusy,
                    _viewModel.IsSpatialAnchorReady,
                    (isBusy, isAnchorReady) => (isBusy, isAnchorReady))
                .Subscribe(state =>
                {
                    if (_spatialAnchorButton != null) _spatialAnchorButton.interactable = !state.isBusy;
                    if (_fixFoodPositionButton != null)
                    {
                        _fixFoodPositionButton.interactable = !state.isBusy && state.isAnchorReady;
                    }
                })
                .AddTo(_disposables);
        }

        private void BindControls()
        {
            if (_apiEndPointUrl != null)
            {
                _apiEndPointUrl.onEndEdit.AddListener(_viewModel.UpdateEndPointUrl);
            }

            if (_foodScaleSlider != null)
            {
                _foodScaleSlider.onValueChanged.AddListener(_viewModel.SetFoodScale);
            }

            if (_testConnectionButton != null)
            {
                _testConnectionButton.OnClickAsObservable()
                    .SubscribeAwait(async (_, ct) => await _viewModel.ConnectionTest(ct))
                    .AddTo(_disposables);
            }

            if (_spatialAnchorButton != null)
            {
                _spatialAnchorButton.OnClickAsObservable()
                    .SubscribeAwait(async (_, ct) => await _viewModel.SetSpatialAnchor(ct))
                    .AddTo(_disposables);
            }

            if (_fixFoodPositionButton != null)
            {
                _fixFoodPositionButton.OnClickAsObservable()
                    .SubscribeAwait(async (_, ct) => await _viewModel.FixFoodPosition(ct))
                    .AddTo(_disposables);
            }

            if (_returnToStartButton != null)
            {
                _returnToStartButton.OnClickAsObservable()
                    .Subscribe(_ => _viewModel.ResetToStart())
                    .AddTo(_disposables);
            }
        }

        private void SetVisible(bool visible)
        {
            if (visible) Show();
            else Hide();
        }

        private void Show()
        {
            if (_canvasGroup == null) return;

            PlaceInFrontOfCamera();
            _visibilityTween?.Kill();
            _visibilityTween = _canvasGroup.DOFade(1f, FadeDuration);
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _interactionGate?.SetEnabled(true);
        }

        private void Hide()
        {
            if (_canvasGroup == null) return;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _interactionGate?.SetEnabled(false);
            ReleaseInputFieldFocus();
            _visibilityTween?.Kill();
            _visibilityTween = _canvasGroup.DOFade(0f, FadeDuration);
        }

        private void PlaceInFrontOfCamera()
        {
            var camera = _targetCamera != null ? _targetCamera : Camera.main;
            if (camera == null || _uiTransform == null) return;

            var cameraTransform = camera.transform;
            _uiTransform.position = cameraTransform.position
                                    + cameraTransform.forward * DisplayDistance;
            _uiTransform.rotation = Quaternion.LookRotation(
                cameraTransform.forward,
                cameraTransform.up);
        }

        private void ReleaseInputFieldFocus()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || _apiEndPointUrl == null) return;
            if (eventSystem.currentSelectedGameObject != _apiEndPointUrl.gameObject) return;
            eventSystem.SetSelectedGameObject(null);
        }

        private void DisableOverlayCanvas()
        {
            if (_overlayCanvas == null) return;
            // World-space Canvas is intentional: an OVROverlay is composited after the
            // scene and would always cover controller geometry. Disabling it also keeps
            // passthrough's underlay slot and the scene depth relationship intact.
            _overlayCanvas.overlayEnabled = false;
            _overlayCanvas.enabled = false;
        }
    }
}
