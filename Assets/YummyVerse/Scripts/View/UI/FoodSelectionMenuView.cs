using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Presentation;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>Unity lifecycle adapter for the menu presentation collaborator.</summary>
    public sealed class FoodSelectionMenuView : MonoBehaviour, IFoodSelectionMenu, IInitializable, System.IDisposable
    {
        public const int ColumnCount = 4;

        private FoodSelectionMenuPresenter _presenter;

        [Inject]
        public void Construct(FoodSelectionMenuPresenter presenter)
        {
            _presenter = presenter;
        }

        public void Initialize()
        {
            _presenter.Initialize(transform);
        }

        public void ShowLoading() => _presenter.ShowLoading();

        public UniTask<FoodCatalogItem> SelectAsync(
            FoodCatalogLoadResult catalog,
            CancellationToken cancellationToken)
        {
            return _presenter.SelectAsync(catalog, cancellationToken);
        }

        public void Hide() => _presenter.Hide();

        private void Update()
        {
            _presenter?.Tick(Time.unscaledDeltaTime);
        }

        public void Dispose()
        {
            _presenter?.Dispose();
        }
    }
}
