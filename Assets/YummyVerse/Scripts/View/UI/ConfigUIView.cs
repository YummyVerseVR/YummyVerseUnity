using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.UI
{
    public class ConfigUIView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField apiEndPointUrl;
        [SerializeField] private Button testConnectionButton;
        [SerializeField] private TextMeshProUGUI lastRequestHttpStatus;
        [SerializeField] private TextMeshProUGUI lastRequestGuid;
        [SerializeField] private Toggle standaloneModeToggle;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private OVROverlayCanvas overlayCanvas;
        [SerializeField] private Slider foodScaleSlider;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform uiTransform;
        [SerializeField] private Button spatialAnchorButton;
        [SerializeField] private Button fixFoodPositionButton;
        [SerializeField] private TextMeshProUGUI spatialPlacementStatus;
        
        private IConfigUIViewModel _configUIViewModel;
        private Tween _visibilityTween;

        private const float FadeDuration = 0.1f;
        private float displayDistanceFromCamera = 0.6f;

        private void Awake()
        {
            // The settings menu starts hidden. Applying this before Start prevents the
            // compositor canvas from affecting the first rendered frame.
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            DisableOverlayCanvas();
        }
        
        [Inject]
        public void Construct(IConfigUIViewModel configUIViewModel)
        {
            _configUIViewModel = configUIViewModel;
        }

        private void Start()
        {
            _configUIViewModel.IsVisible.Subscribe(isVisible =>
            {
                _visibilityTween?.Kill();

                if (isVisible)
                {
                    SetMenuPositionInFrontOfCamera();
                    EnableOverlayCanvas();
                    _visibilityTween = canvasGroup.DOFade(1f, FadeDuration);
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                else
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    _visibilityTween = canvasGroup.DOFade(0f, FadeDuration)
                        .OnComplete(DisableOverlayCanvas);
                }
            }).AddTo(this);
            
            _configUIViewModel.LastRequestHTTPStatus.Subscribe(v =>
            {
                lastRequestHttpStatus.text = "Last Request HTTP Status "
                                             + (_configUIViewModel.IsStandaloneMode.Value ? "(Overridden by Standalone Mode) : " : ": ")
                                             + v;
            }).AddTo(this);

            _configUIViewModel.LastDetectedGuid.Subscribe(v =>
            {
                lastRequestGuid.text = "Last Request GUID " 
                                       + (_configUIViewModel.IsStandaloneMode.Value ? "(Overridden by Standalone Mode) : " : ": ")
                                       + v;
            }).AddTo(this);

            _configUIViewModel.APIEndPointUrl.Subscribe(SetAPIEndPointUrl).AddTo(this);
            
            apiEndPointUrl.onEndEdit.AddListener(v => _configUIViewModel.UpdateEndPointUrl(v));
            _configUIViewModel.APIEndPointUrl.Subscribe(v => apiEndPointUrl.SetTextWithoutNotify(v)).AddTo(this);
            
            standaloneModeToggle.onValueChanged.AddListener(v => _configUIViewModel.SetStandaloneMode(v));
            
            // スライダーの値が変化したら、その値をViewModelに知らせる
            foodScaleSlider.onValueChanged.AddListener(v => _configUIViewModel.SetFoodScale(v));
            
            // ViewModel側で値が設定された場合(スライダーで設定された値が有効である場合や、初期値が設定された場合)、その値をスライダーの見た目に反映する。
            // (SetValueWithoutNotifyを用いて値を設定しているため、onValueChangedは発火しない。)
            _configUIViewModel.FoodScale.Subscribe(v => foodScaleSlider.SetValueWithoutNotify(v)).AddTo(this);

            _configUIViewModel.SpatialPlacementStatus.Subscribe(v =>
            {
                if (spatialPlacementStatus != null) spatialPlacementStatus.text = v;
            }).AddTo(this);

            Observable.CombineLatest(
                    _configUIViewModel.IsSpatialPlacementBusy,
                    _configUIViewModel.IsSpatialAnchorReady,
                    (isBusy, isAnchorReady) => (isBusy, isAnchorReady))
                .Subscribe(state =>
                {
                    if (spatialAnchorButton != null) spatialAnchorButton.interactable = !state.isBusy;
                    if (fixFoodPositionButton != null)
                    {
                        fixFoodPositionButton.interactable = !state.isBusy && state.isAnchorReady;
                    }
                }).AddTo(this);
            
            testConnectionButton.OnClickAsObservable()
                .SubscribeAwait(async (_, ct) => await _configUIViewModel.ConnectionTest(ct))
                .AddTo(this);

            if (spatialAnchorButton != null)
            {
                spatialAnchorButton.OnClickAsObservable()
                    .SubscribeAwait(async (_, ct) => await _configUIViewModel.SetSpatialAnchor(ct))
                    .AddTo(this);
            }

            if (fixFoodPositionButton != null)
            {
                fixFoodPositionButton.OnClickAsObservable()
                    .SubscribeAwait(async (_, ct) => await _configUIViewModel.FixFoodPosition(ct))
                    .AddTo(this);
            }
            
        }

        private void SetAPIEndPointUrl(string url)
        {
            apiEndPointUrl.text = url;
        }

        private void EnableOverlayCanvas()
        {
            if (overlayCanvas == null) return;

            // An underlay requires a black imposter in the eye buffer. It masks the
            // passthrough layer even after CanvasGroup alpha reaches zero.
            overlayCanvas.overlayType = OVROverlay.OverlayType.Overlay;
            overlayCanvas.overlayEnabled = true;
            overlayCanvas.enabled = true;
        }

        private void DisableOverlayCanvas()
        {
            if (overlayCanvas == null) return;
            overlayCanvas.overlayEnabled = false;
            overlayCanvas.enabled = false;
        }

        private void OnDestroy()
        {
            _visibilityTween?.Kill();
            DisableOverlayCanvas();
        }

        private void SetMenuPositionInFrontOfCamera()
        {
            var camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null || uiTransform == null)
            {
                Debug.LogWarning("ConfigUIView: targetCamera or uiTransform is not assigned.");
                return;
            }

            var cameraTransform = camera.transform;
            uiTransform.position = cameraTransform.position + cameraTransform.forward * displayDistanceFromCamera;
            uiTransform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
        }
    }
}
