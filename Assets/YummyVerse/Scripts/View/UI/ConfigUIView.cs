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
        [SerializeField] private Slider foodScaleSlider;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform uiTransform;
        [SerializeField] private Button spatialAnchorButton;
        [SerializeField] private Button fixFoodPositionButton;
        [SerializeField] private TextMeshProUGUI spatialPlacementStatus;
        
        private IConfigUIViewModel _configUIViewModel;

        private float displayDistanceFromCamera = 0.6f;
        
        [Inject]
        public void Construct(IConfigUIViewModel configUIViewModel)
        {
            _configUIViewModel = configUIViewModel;
        }

        private void Start()
        {
            _configUIViewModel.IsVisible.Subscribe(isVisible =>
            {
                if (isVisible)
                {
                    SetMenuPositionInFrontOfCamera();
                    canvasGroup.DOFade(1, 0.1f);
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                else
                {
                    canvasGroup.DOFade(0, 0.1f);
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
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
