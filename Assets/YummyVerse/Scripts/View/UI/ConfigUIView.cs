using System;
using System.Net;
using DG.Tweening;
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
            _configUIViewModel.IsVisible.Subscribe(isVisible =>
            {
                if (isVisible) canvasGroup.DOFade(1, 0.1f);
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
        }
    }
}