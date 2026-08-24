using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IFoodSelectionMenu
    {
        void ShowLoading();
        UniTask<FoodCatalogItem> SelectAsync(FoodCatalogLoadResult catalog, CancellationToken ct);
        void Hide();
    }
}
