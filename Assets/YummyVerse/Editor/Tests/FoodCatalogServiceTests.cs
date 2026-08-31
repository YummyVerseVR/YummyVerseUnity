using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    public sealed class FoodCatalogServiceTests
    {
        [Test]
        public void CombinesRemoteThenPersistentItemsAndRetainsBothErrors()
        {
            var remoteItem = new FoodCatalogItem(
                "api-v2:ramen", "Ramen", "preview", "model", "", MenuItemSource.ApiV2);
            var localItem = new FoodCatalogItem(
                "local:curry", "Curry", "", "curry.glb", "", MenuItemSource.PersistentData);
            var service = new FoodCatalogService(
                new FakeSource(new[] { remoteItem }, "remote failed"),
                new FakeSource(new[] { localItem }, "local failed"));

            var result = service.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.Items[0], Is.SameAs(remoteItem));
            Assert.That(result.Items[1], Is.SameAs(localItem));
            Assert.That(result.ApiError, Does.Contain("remote failed"));
            Assert.That(result.ApiError, Does.Contain("local failed"));
        }

        [Test]
        public void TransportMapperReadsTheChewingSoundFromSampleWavUrl()
        {
            // v2 の PublicMenuItem で規範化されている音声フィールドは sample_wav_url だけ。
            // 拡張子の付かない /wav route もそのまま咀嚼音のURLとして扱う。
            var response = new MenuResponseDto
            {
                items = new[]
                {
                    new MenuItemDto
                    {
                        id = "sushi",
                        display_name = "寿司",
                        available = true,
                        sample_glb_url = "/v2/menu/sushi/glb",
                        sample_wav_url = "/v2/menu/sushi/wav"
                    }
                }
            };

            var items = FoodCatalogTransportMapper.ToCatalogItems(response, "https://example.test/v2");

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].AudioLocation, Is.EqualTo("https://example.test/v2/menu/sushi/wav"));
        }

        [Test]
        public void TransportMapperLeavesAudioEmptyWhenTheMenuOmitsIt()
        {
            var response = new MenuResponseDto
            {
                items = new[]
                {
                    new MenuItemDto
                    {
                        id = "ramen",
                        display_name = "Ramen",
                        available = true,
                        sample_glb_url = "/model.glb"
                    }
                }
            };

            var items = FoodCatalogTransportMapper.ToCatalogItems(response, "https://example.test/v2");

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].AudioLocation, Is.Empty);
            Assert.That(items[0].IsSelectable, Is.True);
        }

        [Test]
        public void TransportMapperRejectsMalformedItemsButKeepsUnavailableItemsAsNonSelectable()
        {
            var response = new MenuResponseDto
            {
                items = new[]
                {
                    null,
                    new MenuItemDto { id = "", display_name = "ignored" },
                    new MenuItemDto
                    {
                        id = "ramen",
                        display_name = "Ramen",
                        available = false,
                        sample_glb_url = "/v2/menu/ramen/model.glb",
                        sample_wav_url = "/v2/menu/ramen/wav"
                    }
                }
            };

            var items = FoodCatalogTransportMapper.ToCatalogItems(
                response,
                "https://example.test/v2");

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].Id, Is.EqualTo("api-v2:ramen"));
            Assert.That(items[0].ModelLocation, Is.EqualTo("https://example.test/v2/menu/ramen/model.glb"));
            Assert.That(items[0].AudioLocation, Is.EqualTo("https://example.test/v2/menu/ramen/wav"));
            Assert.That(items[0].IsSelectable, Is.False);
        }

        [Test]
        public void CatalogServiceStopsBeforePersistentSourceWhenCancelled()
        {
            var cancellation = new CancellationTokenSource();
            var remote = new CancellingSource(cancellation);
            var persistent = new FakeSource(Array.Empty<FoodCatalogItem>(), null);
            var service = new FoodCatalogService(remote, persistent);

            Assert.Throws<OperationCanceledException>(() =>
                service.LoadAsync(cancellation.Token).GetAwaiter().GetResult());
            Assert.That(persistent.LoadCount, Is.Zero);
        }

        private sealed class FakeSource : IRemoteFoodCatalogSource, IPersistentFoodCatalogSource
        {
            private readonly IReadOnlyList<FoodCatalogItem> _items;
            private readonly string _error;

            public FakeSource(IReadOnlyList<FoodCatalogItem> items, string error)
            {
                _items = items;
                _error = error;
            }

            public int LoadCount { get; private set; }

            public UniTask<FoodCatalogSourceResult> LoadAsync(CancellationToken cancellationToken)
            {
                LoadCount++;
                return UniTask.FromResult(new FoodCatalogSourceResult(_items, _error));
            }

            public bool TrySelectRandom(out FoodCatalogItem item)
            {
                item = _items.Count == 0 ? null : _items[0];
                return item != null;
            }
        }

        private sealed class CancellingSource : IRemoteFoodCatalogSource
        {
            private readonly CancellationTokenSource _cancellation;

            public CancellingSource(CancellationTokenSource cancellation)
            {
                _cancellation = cancellation;
            }

            public async UniTask<FoodCatalogSourceResult> LoadAsync(CancellationToken cancellationToken)
            {
                _cancellation.Cancel();
                await UniTask.Yield(cancellationToken);
                return FoodCatalogSourceResult.Empty();
            }
        }
    }
}
