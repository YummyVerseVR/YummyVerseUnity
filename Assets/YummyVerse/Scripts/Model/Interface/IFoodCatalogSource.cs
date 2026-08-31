using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// A catalog source returns metadata only. It must not create a runtime food model.
    /// Keeping this port small makes source composition independent of Unity transport APIs.
    /// </summary>
    public interface IFoodCatalogSource
    {
        UniTask<FoodCatalogSourceResult> LoadAsync(CancellationToken cancellationToken);
    }

    /// <summary>Remote/API catalog boundary.</summary>
    public interface IRemoteFoodCatalogSource : IFoodCatalogSource
    {
    }

    /// <summary>Persistent local catalog boundary, including the local selection policy.</summary>
    public interface IPersistentFoodCatalogSource : IFoodCatalogSource
    {
        bool TrySelectRandom(out FoodCatalogItem item);
    }
}
