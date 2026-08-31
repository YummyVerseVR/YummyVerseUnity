using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodCatalogService
    {
        UniTask<FoodCatalogLoadResult> LoadAsync(CancellationToken ct);
    }
}
