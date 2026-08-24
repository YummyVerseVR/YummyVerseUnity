using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public sealed class FoodLoaderRouter : IFoodFetchable
    {
        private readonly IFoodFetchable _localLoader;
        private readonly IFoodFetchable _networkLoader;

        public FoodLoaderRouter(IFoodFetchable localLoader, IFoodFetchable networkLoader)
        {
            _localLoader = localLoader;
            _networkLoader = networkLoader;
        }

        public UniTask<FoodDownloadResult> Download(MenuItem item, CancellationToken ct)
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
                ? _networkLoader.Download(item, ct)
                : _localLoader.Download(item, ct);
        }
    }
}
