using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// Selects an already-composed model adapter. The router owns no factories and
    /// therefore cannot create a new loader for every menu selection.
    /// </summary>
    public sealed class FoodLoaderRouter : IFoodModelLoader
    {
        private readonly ILocalFoodModelLoader _localLoader;
        private readonly INetworkFoodModelLoader _networkLoader;

        public FoodLoaderRouter(
            ILocalFoodModelLoader localLoader,
            INetworkFoodModelLoader networkLoader)
        {
            _localLoader = localLoader ?? throw new System.ArgumentNullException(nameof(localLoader));
            _networkLoader = networkLoader ?? throw new System.ArgumentNullException(nameof(networkLoader));
        }

        public UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken ct)
        {
            if (!item.IsValid)
            {
                return UniTask.FromResult(new FoodDownloadResult
                {
                    RequestedGuid = item.Guid,
                    RequestedItemId = item.Id,
                    StatusCode = HttpStatusCode.BadRequest
                });
            }

            return item.Source == MenuItemSource.ApiV2
                ? _networkLoader.LoadAsync(item, ct)
                : _localLoader.LoadAsync(item, ct);
        }
    }
}
