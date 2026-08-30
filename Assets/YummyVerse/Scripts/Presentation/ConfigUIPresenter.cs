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
        private readonly IYummyServiceV2ConfigViewModel _v2ViewModel;
        private readonly CompositeDisposable _disposables = new();
        private Tween _visibilityTween;
        private PointableCanvasInteractionGate _interactionGate;
        private CanvasGroup _canvasGroup;
        private TMP_InputField _apiEndPointUrl;
        private TMP_InputField _apiDeviceToken;
        private Button _testConnectionButton;
        private OVROverlayCanvas _overlayCanvas;
        private Slider _foodScaleSlider;
        private Camera _targetCamera;
        private Transform _uiTransform;
        private Button _spatialAnchorButton;
        private Button _fixFoodPositionButton;
        private TextMeshProUGUI _spatialPlacementStatus;
        private Button _returnToStartButton;
        private IVirtualKeyboard _virtualKeyboard;
        private IMultiFieldVirtualKeyboard _multiFieldKeyboard;
        private bool _initialized;

        public ConfigUIPresenter(IConfigUIViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _v2ViewModel = viewModel as IYummyServiceV2ConfigViewModel;
        }

        /// <summary>
        /// Backward-compatible initializer for scenes that have not added the v2
        /// token field yet.  The new overload below is used by the updated settings
        /// view when its serialized token reference is available.
        /// </summary>
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
            Button returnToStartButton,
            IVirtualKeyboard virtualKeyboard = null)
        {
            Initialize(
                apiEndPointUrl,
                null,
                testConnectionButton,
                canvasGroup,
                overlayCanvas,
                foodScaleSlider,
                targetCamera,
                uiTransform,
                spatialAnchorButton,
                fixFoodPositionButton,
                spatialPlacementStatus,
                returnToStartButton,
                virtualKeyboard);
        }

        public void Initialize(
            TMP_InputField apiEndPointUrl,
            TMP_InputField apiDeviceToken,
            Button testConnectionButton,
            CanvasGroup canvasGroup,
            OVROverlayCanvas overlayCanvas,
            Slider foodScaleSlider,
            Camera targetCamera,
            Transform uiTransform,
            Button spatialAnchorButton,
            Button fixFoodPositionButton,
            TextMeshProUGUI spatialPlacementStatus,
            Button returnToStartButton,
            IVirtualKeyboard virtualKeyboard = null)
        {
            if (_initialized) return;
            _initialized = true;

            _virtualKeyboard = virtualKeyboard;
            if (_virtualKeyboard != null) _virtualKeyboard.EditingFinished += CommitEndPointUrl;
            _multiFieldKeyboard = virtualKeyboard as IMultiFieldVirtualKeyboard;
            if (_multiFieldKeyboard != null)
            {
                _multiFieldKeyboard.EditingFinishedForField += HandleVirtualKeyboardFinished;
            }

            _apiEndPointUrl = apiEndPointUrl;
            _apiDeviceToken = apiDeviceToken;
            if (_apiDeviceToken != null)
            {
                _apiDeviceToken.contentType = TMP_InputField.ContentType.Password;
            }
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
            if (_virtualKeyboard != null) _virtualKeyboard.EditingFinished -= CommitEndPointUrl;
            if (_multiFieldKeyboard != null)
            {
                _multiFieldKeyboard.EditingFinishedForField -= HandleVirtualKeyboardFinished;
            }
            if (_apiEndPointUrl != null)
            {
                _apiEndPointUrl.onEndEdit.RemoveListener(HandleEndPointUrlEndEdit);
            }
            if (_apiDeviceToken != null)
            {
                _apiDeviceToken.onEndEdit.RemoveListener(HandleDeviceTokenEndEdit);
            }
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
            if (_v2ViewModel != null)
            {
                _v2ViewModel.APIDeviceToken
                    .Subscribe(value => _apiDeviceToken?.SetTextWithoutNotify(value))
                    .AddTo(_disposables);
            }
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
                _apiEndPointUrl.onEndEdit.AddListener(HandleEndPointUrlEndEdit);
            }

            if (_apiDeviceToken != null && _v2ViewModel != null)
            {
                _apiDeviceToken.onEndEdit.AddListener(HandleDeviceTokenEndEdit);
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

        /// <summary>編集を確定してエンドポイントに反映する。仮想キーボードを閉じたときに呼ばれる。</summary>
        public void CommitEndPointUrl(string url)
        {
            _viewModel.UpdateEndPointUrl(url);
        }

        private void HandleDeviceTokenEndEdit(string token)
        {
            // A shared VirtualKeyboardView emits its field-aware event when the
            // keyboard closes.  Ignore the intermediate TMP onEndEdit notification
            // so validation happens exactly once after virtual typing is complete.
            if (_virtualKeyboard != null && _virtualKeyboard.IsEditing) return;
            _v2ViewModel?.UpdateDeviceAccessToken(token);
        }

        private void HandleVirtualKeyboardFinished(TMP_InputField field, string value)
        {
            if (_v2ViewModel == null || field != _apiDeviceToken) return;
            _v2ViewModel.UpdateDeviceAccessToken(value);
        }

        /// <remarks>
        /// 仮想キーボードはキーを押すたびに入力欄のフォーカスを奪う
        /// (PointableCanvasModule.ProcessPress → DeselectIfSelectionChanged)。
        /// TMP_InputField はフォーカスを失うと onEndEdit を飛ばすので、これをそのまま
        /// 確定として扱うと1文字打つごとに URL 検証エラーのダイアログが出てしまう。
        /// キーボードが開いている間は確定せず、閉じたときに
        /// <see cref="CommitEndPointUrl"/> で一度だけ確定する。
        /// 物理キーボードで打つ場合はキーボードが開かないので、ここがそのまま確定になる。
        /// </remarks>
        private void HandleEndPointUrlEndEdit(string url)
        {
            if (_virtualKeyboard != null && _virtualKeyboard.IsEditing) return;
            _viewModel.UpdateEndPointUrl(url);
        }

        private void Hide()
        {
            if (_canvasGroup == null) return;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _interactionGate?.SetEnabled(false);

            // キーボードはパネルの CanvasGroup の外にいるのでフェードで消えない。
            // フォーカスがキーの上にあると入力欄の onDeselect も飛ばないので、明示的に閉じる。
            _virtualKeyboard?.Close();
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
            if (eventSystem.currentSelectedGameObject != _apiEndPointUrl.gameObject
                && (_apiDeviceToken == null
                    || eventSystem.currentSelectedGameObject != _apiDeviceToken.gameObject)) return;
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
