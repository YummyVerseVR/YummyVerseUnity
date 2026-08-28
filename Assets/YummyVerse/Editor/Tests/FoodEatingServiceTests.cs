using NUnit.Framework;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    public class FoodEatingServiceTests
    {
        private sealed class RecordingPublisher : IGameEventPublisher
        {
            public int ScoopedCount { get; private set; }
            public int DishClearedCount { get; private set; }

            public void PublishFoodScooped() => ScoopedCount++;
            public void PublishDishCleared() => DishClearedCount++;
            public void PublishMenuItemSelected(MenuItem item) { }
            public void PublishUserAbsent() { }
            public void ResetSessionState() { }
        }

        [Test]
        public void DefaultPortionCount_IsFive()
        {
            var service = new FoodEatingService(new RecordingPublisher());
            Assert.That(service.TotalPortions, Is.EqualTo(5));
            Assert.That(FoodEatingService.DefaultPortionsPerFood, Is.EqualTo(5));
        }

        [Test]
        public void WithoutFood_ScoopIsIgnored()
        {
            var publisher = new RecordingPublisher();
            var service = new FoodEatingService(publisher);

            Assert.That(service.TryScoop(), Is.False);
            Assert.That(service.IsInteractable.CurrentValue, Is.False);
            Assert.That(publisher.ScoopedCount, Is.Zero);
        }

        [Test]
        public void RemainingFraction_ReachesZeroExactlyAtTheConfiguredScoopCount()
        {
            var publisher = new RecordingPublisher();
            var service = new FoodEatingService(publisher, 5);
            service.BeginFood();

            Assert.That(service.RemainingFraction.CurrentValue, Is.EqualTo(1f));

            var expected = new[] { 0.8f, 0.6f, 0.4f, 0.2f, 0f };
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(service.TryScoop(), Is.True);
                Assert.That(service.RemainingFraction.CurrentValue, Is.EqualTo(expected[i]).Within(1e-5f));
            }

            Assert.That(publisher.ScoopedCount, Is.EqualTo(5));
            Assert.That(publisher.DishClearedCount, Is.EqualTo(1));
        }

        [Test]
        public void AfterClearing_FurtherScoopsPublishNothing()
        {
            var publisher = new RecordingPublisher();
            var service = new FoodEatingService(publisher, 2);
            service.BeginFood();

            service.TryScoop();
            service.TryScoop();

            Assert.That(service.IsInteractable.CurrentValue, Is.False);
            Assert.That(service.TryScoop(), Is.False);
            Assert.That(publisher.ScoopedCount, Is.EqualTo(2));
            Assert.That(publisher.DishClearedCount, Is.EqualTo(1));
        }

        [Test]
        public void AbandonFood_DoesNotCountAsClearing()
        {
            var publisher = new RecordingPublisher();
            var service = new FoodEatingService(publisher, 5);
            service.BeginFood();
            service.TryScoop();

            service.AbandonFood();

            Assert.That(publisher.DishClearedCount, Is.Zero);
            Assert.That(service.IsInteractable.CurrentValue, Is.False);
            Assert.That(service.RemainingFraction.CurrentValue, Is.EqualTo(1f));
            Assert.That(service.TryScoop(), Is.False);
        }

        [Test]
        public void BeginFood_RefillsForTheNextVisitor()
        {
            var publisher = new RecordingPublisher();
            var service = new FoodEatingService(publisher, 3);
            service.BeginFood();
            service.TryScoop();
            service.TryScoop();
            service.TryScoop();

            service.BeginFood();

            Assert.That(service.RemainingFraction.CurrentValue, Is.EqualTo(1f));
            Assert.That(service.IsInteractable.CurrentValue, Is.True);
            Assert.That(service.TryScoop(), Is.True);
        }

        [Test]
        public void ForceClear_FinishesThroughTheSameEventPathExactlyOnce()
        {
            var publisher = new RecordingPublisher();
            var service = new FoodEatingService(publisher, 5);
            service.BeginFood();
            service.TryScoop();

            Assert.That(service.ForceClear(), Is.True);
            Assert.That(service.RemainingFraction.CurrentValue, Is.EqualTo(0f));
            Assert.That(publisher.DishClearedCount, Is.EqualTo(1));
            Assert.That(publisher.ScoopedCount, Is.EqualTo(5));
            Assert.That(service.ForceClear(), Is.False);
        }
    }
}
