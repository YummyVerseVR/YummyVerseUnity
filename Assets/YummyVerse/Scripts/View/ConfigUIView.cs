using System;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;
using R3;

namespace YummyVerse.Scripts.View
{
    public class ConfigUIView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField apiEndPointUrl;
        [SerializeField] private Button testConnectionButton;
        [SerializeField] private TextMeshProUGUI lastRequestHttpStatus;
        [SerializeField] private TextMeshProUGUI lastRequestGuid;
        [SerializeField] private Toggle standaloneModeToggle;
        [SerializeField] private CanvasGroup canvasGroup;
        
        private IConfigUIViewModel _configUIViewModel;

        [Inject]
        public void Construct(IConfigUIViewModel configUIViewModel)
        {
            _configUIViewModel = configUIViewModel;
        }

        private void Start()
        {
            _configUIViewModel.IsVisible.Subscribe(v =>
            {
                canvasGroup
            });
            
            _configUIViewModel.LastRequestHTTPStatus.Subscribe(v =>
            {
                lastRequestHttpStatus.text = "Last Request HTTP Status : " + v;
            }).AddTo(this);

            _configUIViewModel.LastRequestGuid.Subscribe(v =>
            {
                lastRequestGuid.text = "Last Request GUID : " + v;
            }).AddTo(this);

            Observable.FromEvent(
                    h => _configUIViewModel.OnAPIEndPointValidationError += h,
                    h => _configUIViewModel.OnAPIEndPointValidationError -= h)
                .Subscribe(_ => ShowAPIEndPointValidationError()).AddTo(this);

            _configUIViewModel.ConnectionTestResult.Subscribe(ShowTestConnectionResult).AddTo(this);
            
            apiEndPointUrl.onEndEdit.AddListener(v => _configUIViewModel.UpdateEndPointUrl(v));
        }

        // APIバリデーションのエラーダイアログを表示する
        private void ShowAPIEndPointValidationError()
        {
            
        }
        
        // Test Connectionの結果のダイアログを表示する
        private void ShowTestConnectionResult(HttpStatusCode httpStatusCode)
        {
            
        }
        
    }
}