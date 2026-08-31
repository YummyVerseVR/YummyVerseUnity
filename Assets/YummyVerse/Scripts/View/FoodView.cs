using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Presentation;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View
{
    /// <summary>
    /// Unity lifecycle adapter for the food runtime presentation.
    /// Serialized settings and callbacks stay here; food policy and object ownership
    /// are delegated to <see cref="FoodRuntimePresenter"/>.
    /// </summary>
    public sealed class FoodView : MonoBehaviour
    {
        [SerializeField] private ScoopDetectionSettings _scoopSettings = new();
        [SerializeField] private ScoopCrumbEffectSettings _crumbEffectSettings = new();

        private IFoodViewModel _foodViewModel;
        private IFoodEatingService _foodEatingService;
        private FoodRuntimePresenter _presenter;

        [Inject]
        public void Construct(
            IFoodViewModel foodViewModel,
            IFoodEatingService foodEatingService,
            FoodRuntimePresenter presenter)
        {
            _foodViewModel = foodViewModel;
            _foodEatingService = foodEatingService;
            _presenter = presenter;
        }

        private void Start()
        {
            _presenter.Initialize(_scoopSettings, _crumbEffectSettings);
            _foodViewModel.foodGltf
                .SubscribeAwait(async (gltfImport, cancellationToken) =>
                {
                    await DisplayFoodAsync(gltfImport, cancellationToken);
                })
                .AddTo(this);
            _foodViewModel.foodTransform
                .Subscribe(_presenter.SetFoodTransform)
                .AddTo(this);
            _foodViewModel.foodScale
                .Subscribe(_presenter.SetBaseScale)
                .AddTo(this);
            _foodEatingService.RemainingFraction
                .Subscribe(_presenter.SetRemainingFraction)
                .AddTo(this);
            _foodViewModel.OnFoodResetRequested += _presenter.ResetFoodState;
        }

        private void LateUpdate()
        {
            _presenter.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_foodViewModel != null)
            {
                _foodViewModel.OnFoodResetRequested -= _presenter.ResetFoodState;
            }

            _presenter?.Dispose();
        }

        private UniTask DisplayFoodAsync(GLTFast.GltfImport gltfImport, CancellationToken cancellationToken)
        {
            return _presenter.DisplayAsync(
                gltfImport,
                _foodViewModel.foodTransform.Value,
                cancellationToken);
        }
    }
}
