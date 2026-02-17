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
        
        private float displayDistanceFromCamera = 0.6f;
        
        
        private IConfigUIViewModel _configUIViewModel;

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
                }
                else canvasGroup.DOFade(0, 0.1f);
            }).AddTo(this);
            
            _configUIViewModel.LastRequestHTTPStatus.Subscribe(v =>
            {
                lastRequestHttpStatus.text = "Last Request HTTP Status : " + v;
            }).AddTo(this);

            _configUIViewModel.LastRequestGuid.Subscribe(v =>
            {
                lastRequestGuid.text = "Last Request GUID : " + v;
            }).AddTo(this);
            
            apiEndPointUrl.onEndEdit.AddListener(v => _configUIViewModel.UpdateEndPointUrl(v));
            
            standaloneModeToggle.onValueChanged.AddListener(v => _configUIViewModel.SetStandaloneMode(v));
            
            foodScaleSlider.onValueChanged.AddListener(v => _configUIViewModel.SetFoodScale(v));
        }

        private void SetMenuPositionInFrontOfCamera()
        {
            if (targetCamera == null || uiTransform == null)
            {
                Debug.LogWarning("ConfigUIView: targetCamera or uiTransform is not assigned.");
                return;
            }

            var cameraTransform = targetCamera.transform;
            uiTransform.position = cameraTransform.position + cameraTransform.forward * displayDistanceFromCamera;
            uiTransform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
        }
    }
}
