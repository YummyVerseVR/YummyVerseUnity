using System.Net;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;
using R3;

namespace YummyVerse.Scripts.ViewModel
{
    public class TestConnectionResultView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button okButton;
        [SerializeField] private TextMeshProUGUI statusCode;
        [SerializeField] private TextMeshProUGUI statusDescription;
        
        private IConfigUIViewModel _configUIViewModel;

        [Inject]
        public void Construct(IConfigUIViewModel configUIViewModel)
        {
            _configUIViewModel = configUIViewModel;
        }

        private void Start()
        {
            _configUIViewModel.ConnectionTestResult.Subscribe(ShowStatusDialog).AddTo(this);
            
            okButton.onClick.AddListener(OnClickOk);
        }

        private void ShowStatusDialog(HttpStatusCode status)
        {
            canvasGroup.DOFade(1, 0.1f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            statusCode.text = ((int)status).ToString();
            statusDescription.text = status.ToString();
        }
        
        private void OnClickOk()
        {
            canvasGroup.DOFade(0, 0.1f);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}