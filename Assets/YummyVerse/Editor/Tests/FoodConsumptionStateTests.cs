using System;
using NUnit.Framework;
using YummyVerse.Scripts.Model;

namespace YummyVerse.Editor.Tests
{
    public class FoodConsumptionStateTests
    {
        [Test]
        public void Constructor_RejectsNonPositivePortionCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FoodConsumptionState(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FoodConsumptionState(-1));
        }

        [Test]
        public void TryConsume_DecreasesExactlyOnePortionAndFractionMonotonically()
        {
            var state = new FoodConsumptionState(4);

            Assert.That(state.TryConsume(out var first), Is.True);
            Assert.That(first.RemainingPortions, Is.EqualTo(3));
            Assert.That(first.RemainingFraction, Is.EqualTo(0.75f));
            Assert.That(first.DishCleared, Is.False);

            Assert.That(state.TryConsume(out var second), Is.True);
            Assert.That(second.RemainingPortions, Is.EqualTo(2));
            Assert.That(second.RemainingFraction, Is.EqualTo(0.5f));
            Assert.That(second.RemainingFraction, Is.LessThan(first.RemainingFraction));
            Assert.That(second.DishCleared, Is.False);
        }

        [Test]
        public void TryConsume_ClearsOnceAndNeverDropsBelowZero()
        {
            var state = new FoodConsumptionState(1);

            Assert.That(state.TryConsume(out var cleared), Is.True);
            Assert.That(cleared.WasConsumed, Is.True);
            Assert.That(cleared.DishCleared, Is.True);
            Assert.That(cleared.RemainingPortions, Is.Zero);
            Assert.That(cleared.RemainingFraction, Is.Zero);

            Assert.That(state.TryConsume(out var duplicate), Is.False);
            Assert.That(duplicate.WasConsumed, Is.False);
            Assert.That(duplicate.DishCleared, Is.False);
            Assert.That(duplicate.RemainingPortions, Is.Zero);
            Assert.That(state.RemainingPortions, Is.Zero);
        }
    }
}
