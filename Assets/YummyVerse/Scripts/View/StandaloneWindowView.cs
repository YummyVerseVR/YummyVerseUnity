using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View
{
    public class StandaloneWindowView : MonoBehaviour
    {
        private IStandaloneWindowViewModel _viewModel;
        
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button shrimpButton;
        [SerializeField] private Button curryButton;
        [SerializeField] private Button hamburgButton;

        [Inject]
        private void Construct(IStandaloneWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void Start()
        {
            _viewModel.IsVisible.Subscribe(isVisible =>
            {
                if(isVisible)  ShowWindow();
                else HideWindow();
            }).AddTo(this);
        }
        private void ShowWindow()
        {
            canvasGroup.DOFade(1, 0.1f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void HideWindow()
        {
            canvasGroup.DOFade(0, 0.1f);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}