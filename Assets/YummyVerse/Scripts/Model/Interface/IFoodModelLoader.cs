using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>Application-facing loader that routes a selected menu item to one adapter.</summary>
    public interface IFoodModelLoader
    {
        UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken cancellationToken);
    }

    /// <summary>Local/built-in and persistent-file model adapter contract.</summary>
    public interface ILocalFoodModelLoader
    {
        UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken cancellationToken);
    }

    /// <summary>Remote/API model adapter contract.</summary>
    public interface INetworkFoodModelLoader
    {
        UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken cancellationToken);
    }
}
