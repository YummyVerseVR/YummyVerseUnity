using System;
using System.Collections.Generic;
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
            var context = new FoodContext(events, fetcher);
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
            var context = new FoodContext(events, fetcher);

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
            var context = new FoodContext(events, fetcher);

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

        [Test]
        public void Preparation_EndsBeforeTheDownloadResultIsPublished()
        {
            // ドームを消して煙を出す表示側は準備中フラグを見ているため、
            // 食べ物が流れてくる前に降ろさないと、煙より先に食べ物が出てしまう。
            var events = new FakeGameEventBus();
            var fetcher = new RecordingFoodFetcher();
            var context = new FoodContext(events, fetcher);
            var order = new List<string>();

            try
            {
                context.Initialize();
                context.BeginPreparation();
                Assert.That(context.IsPreparing.CurrentValue, Is.True);

                using var preparation = context.IsPreparing
                    .Skip(1)
                    .Subscribe(_ => order.Add("preparation"));
                using var download = context.downloadResult
                    .Skip(1)
                    .Subscribe(_ => order.Add("download"));

                events.RaiseMenuItemSelected(new MenuItem(LocalFoods.Curry, Guid.NewGuid()));

                Assert.That(context.IsPreparing.CurrentValue, Is.False);
                Assert.That(order, Is.EqualTo(new[] { "preparation", "download" }));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Reset_EndsPreparation()
        {
            // セッションが終わったのにドームが被さったままにならないようにする。
            var events = new FakeGameEventBus();
            var fetcher = new RecordingFoodFetcher();
            var context = new FoodContext(events, fetcher);

            try
            {
                context.Initialize();
                context.BeginPreparation();
                context.Reset();

                Assert.That(context.IsPreparing.CurrentValue, Is.False);
            }
            finally
            {
                context.Dispose();
            }
        }

        private sealed class RecordingFoodFetcher : IFoodModelLoader
        {
            public int DownloadCount { get; private set; }
            public MenuItem LastRequestedItem { get; private set; }

            public UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken ct)
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
