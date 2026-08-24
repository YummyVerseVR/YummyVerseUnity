using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Editor.Tests
{
    public class FoodContextTests
    {
        [Test]
        public void MenuSelection_LoadsTheSelectedStandaloneItem()
        {
            var events = new FakeGameEventBus();
            var fetcher = new RecordingFoodFetcher();
            var context = new FoodContext(events, new FakeFoodFetcherFactory(fetcher));
            var selectedGuid = Guid.NewGuid();

            try
            {
                context.Initialize();
                events.RaiseMenuItemSelected(new MenuItem(LocalFoods.Curry, selectedGuid));

                Assert.That(fetcher.DownloadCount, Is.EqualTo(1));
                Assert.That(fetcher.LastRequestedItem.Guid, Is.EqualTo(selectedGuid));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void EmptyMenuIdentity_DoesNotStartALoad()
        {
            var events = new FakeGameEventBus();
            var fetcher = new RecordingFoodFetcher();
            var context = new FoodContext(events, new FakeFoodFetcherFactory(fetcher));

            try
            {
                context.Initialize();
                events.RaiseMenuItemSelected(new MenuItem(LocalFoods.Curry, Guid.Empty));

                Assert.That(fetcher.DownloadCount, Is.Zero);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void QrDetection_DoesNotStartAFoodLoad()
        {
            var events = new FakeGameEventBus();
            var fetcher = new RecordingFoodFetcher();
            var context = new FoodContext(events, new FakeFoodFetcherFactory(fetcher));

            try
            {
                context.Initialize();
                events.RaiseQrPlateDetected();

                Assert.That(fetcher.DownloadCount, Is.Zero);
            }
            finally
            {
                context.Dispose();
            }
        }

        private sealed class FakeFoodFetcherFactory : IFoodFetchableFactory
        {
            private readonly IFoodFetchable _fetcher;

            public FakeFoodFetcherFactory(IFoodFetchable fetcher)
            {
                _fetcher = fetcher;
            }

            public IFoodFetchable Create() => _fetcher;
        }

        private sealed class RecordingFoodFetcher : IFoodFetchable
        {
            public int DownloadCount { get; private set; }
            public MenuItem LastRequestedItem { get; private set; }

            public UniTask<FoodDownloadResult> Download(MenuItem item, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                DownloadCount++;
                LastRequestedItem = item;
                return UniTask.FromResult(new FoodDownloadResult
                {
                    RequestedGuid = item.Guid,
                    RequestedItemId = item.Id
                });
            }
        }

#pragma warning disable CS0067 // Interface completeness: these events are intentionally unused by this focused fake.
        private sealed class FakeGameEventBus : IGameEventBus
        {
            public event Action OnStartButtonPressed;
            public event Action OnQrPlateDetected;
            public event Action OnQrPlateLost;
            public event Action OnFoodScooped;
            public event Action OnDishCleared;
            public event Action<MenuItem> OnMenuItemSelected;
            public event Action OnUserAbsent;

            public Observable<GameEventId> OnAnyEvent => throw new NotSupportedException();
            public MenuItem? LastSelectedMenuItem { get; private set; }

            public Observable<Unit> GetStream(GameEventId id) => throw new NotSupportedException();

            public void RaiseMenuItemSelected(MenuItem item)
            {
                LastSelectedMenuItem = item;
                OnMenuItemSelected?.Invoke(item);
            }

            public void RaiseQrPlateDetected()
            {
                OnQrPlateDetected?.Invoke();
            }
        }
#pragma warning restore CS0067
    }
}
