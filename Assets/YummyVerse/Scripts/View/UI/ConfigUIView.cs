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
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private OVROverlayCanvas overlayCanvas;
        [SerializeField] private Slider foodScaleSlider;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform uiTransform;
        [SerializeField] private Button spatialAnchorButton;
        [SerializeField] private Button fixFoodPositionButton;
        [SerializeField] private TextMeshProUGUI spatialPlacementStatus;
        [SerializeField] private Button returnToStartButton;
        
        private IConfigUIViewModel _configUIViewModel;
        private Tween _visibilityTween;
        private PointableCanvasInteractionGate _interactionGate;

        private const float FadeDuration = 0.1f;
        private float displayDistanceFromCamera = 0.6f;

        private void Awake()
        {
            // Keep the pointer visual inside the canvas texture.  The canvas is
            // submitted as an underlay (see EnableOverlayCanvas), so scene geometry
            // such as the controller still participates in depth ordering.
            if (!TryGetComponent<PointableCanvasPointerVisual>(out _))
            {
                gameObject.AddComponent<PointableCanvasPointerVisual>();
            }

            // The settings menu starts hidden. Applying this before Start prevents the
            // compositor canvas from affecting the first rendered frame.
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 閉じている設定画面はカメラ前(0.6m)に残ったままなので、Interactable を切らないと
            // その奥(0.8m)に出るチュートリアルのダイアログよりも手前でレイを奪ってしまう。
            // 実際の開閉は Start の IsVisible 購読(初期値 false)から行う。
            _interactionGate = new PointableCanvasInteractionGate(canvasGroup);

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
                    _interactionGate.SetEnabled(true);
                }
                else
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    _interactionGate.SetEnabled(false);
                    _visibilityTween = canvasGroup.DOFade(0f, FadeDuration)
                        .OnComplete(DisableOverlayCanvas);
                }
            }).AddTo(this);
            
            _configUIViewModel.APIEndPointUrl.Subscribe(SetAPIEndPointUrl).AddTo(this);
            
            apiEndPointUrl.onEndEdit.AddListener(v => _configUIViewModel.UpdateEndPointUrl(v));
            _configUIViewModel.APIEndPointUrl.Subscribe(v => apiEndPointUrl.SetTextWithoutNotify(v)).AddTo(this);
            
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

            if (returnToStartButton != null)
            {
                returnToStartButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        // ResetToStart aborts the active session and lets the session
                        // controller perform the full food/state cleanup. Hide the
                        // settings overlay immediately so the attract message can be
                        // seen while that reset completes.
                        _configUIViewModel.SetVisible(false);
                        _configUIViewModel.ResetToStart();
                    })
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

            // An Overlay is composited after the eye buffer and therefore always
            // appears in front of scene objects, including the controller.  The
            // settings panel is a world-space UI, so submit it as an Underlay and
            // let the scene depth determine which object is in front.
            overlayCanvas.overlayType = OVROverlay.OverlayType.Underlay;
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

        private void OnDisable()
        {
            // CanvasGroupで隠す通常経路以外に、UIのGameObject自体が無効化される
            // 経路でも配置プレビューを残さない。
            if (_configUIViewModel != null)
            {
                _configUIViewModel.SetVisible(false);
            }
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
