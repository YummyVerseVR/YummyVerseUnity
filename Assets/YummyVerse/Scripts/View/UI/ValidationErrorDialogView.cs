using System;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.ViewModel
{
    public class ValidationErrorDialogView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button _okButton;
        
        private IConfigUIViewModel _configUIViewModel;

        [Inject]
        public void Construct(IConfigUIViewModel configUIViewModel)
        {
            _configUIViewModel = configUIViewModel;
        }

        private void Start()
        {
            Observable.FromEvent(
                    h => _configUIViewModel.OnAPIEndPointValidationError += h,
                    h => _configUIViewModel.OnAPIEndPointValidationError -= h)
                .Subscribe(_ => ShowAPIEndPointValidationError()).AddTo(this);
            if (_configUIViewModel is IYummyServiceV2ConfigViewModel v2ViewModel)
            {
                Observable.FromEvent(
                        h => v2ViewModel.OnAPIDeviceTokenValidationError += h,
                        h => v2ViewModel.OnAPIDeviceTokenValidationError -= h)
                    .Subscribe(_ => ShowAPIEndPointValidationError()).AddTo(this);
            }
            _okButton.onClick.AddListener(OnClickOk);
        }
        
        private void ShowAPIEndPointValidationError()
        {
            canvasGroup.DOFade(1, 0.1f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void OnClickOk()
        {
            canvasGroup.DOFade(0, 0.1f);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
    }
}
