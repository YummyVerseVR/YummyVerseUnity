using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Editor.Tests
{
    public sealed class FoodLoaderRouterTests
    {
        [Test]
        public void RoutesBuiltInAndPersistentItemsToTheLocalAdapter()
        {
            var local = new RecordingLoader();
            var network = new RecordingLoader();
            var router = new FoodLoaderRouter(local, network);

            var item = new MenuItem(LocalFoods.Curry, Guid.NewGuid());
            router.LoadAsync(item, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(local.LoadCount, Is.EqualTo(1));
            Assert.That(network.LoadCount, Is.Zero);
        }

        [Test]
        public void RoutesApiItemsToTheRemoteAdapter()
        {
            var local = new RecordingLoader();
            var network = new RecordingLoader();
            var router = new FoodLoaderRouter(local, network);

            var item = new MenuItem(new FoodCatalogItem(
                "api-v2:sushi", "Sushi", "", "https://example.test/sushi.glb", MenuItemSource.ApiV2));
            router.LoadAsync(item, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(local.LoadCount, Is.Zero);
            Assert.That(network.LoadCount, Is.EqualTo(1));
        }

        [Test]
        public void InvalidItemFailsBeforeCallingAnAdapter()
        {
            var local = new RecordingLoader();
            var network = new RecordingLoader();
            var result = new FoodLoaderRouter(local, network)
                .LoadAsync(default, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(local.LoadCount, Is.Zero);
            Assert.That(network.LoadCount, Is.Zero);
        }

        private sealed class RecordingLoader : ILocalFoodModelLoader, INetworkFoodModelLoader
        {
            public int LoadCount { get; private set; }

            public UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken cancellationToken)
            {
                LoadCount++;
                return UniTask.FromResult(new FoodDownloadResult
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestedGuid = item.Guid,
                    RequestedItemId = item.Id
                });
            }
        }
    }
}
