using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.UI
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
            shrimpButton.OnClickAsObservable().Subscribe(_ => _viewModel.SpawnLocalFood(LocalFoods.Shrimp)).AddTo(this);
            curryButton.OnClickAsObservable().Subscribe(_ => _viewModel.SpawnLocalFood(LocalFoods.Curry)).AddTo(this);
            hamburgButton.OnClickAsObservable().Subscribe(_ => _viewModel.SpawnLocalFood(LocalFoods.Hamburg)).AddTo(this);
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