using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;
using YummyVerse.Scripts.Model;

namespace YummyVerse.Editor.Tests
{
    public class FoodInteractionBoundsCalculatorTests
    {
        [Test]
        public void EmptyPointSet_IsNotInteractionReady()
        {
            Assert.That(
                FoodInteractionBoundsCalculator.TryCalculateFromLocalPoints(new Vector3[0], out _),
                Is.False);
            Assert.That(
                FoodInteractionBoundsCalculator.TryCalculateFromLocalPoints(null, out _),
                Is.False);
        }

        [Test]
        public void Bounds_SpanTheTwoExtremeCorners()
        {
            var points = new[]
            {
                new Vector3(-1f, 0f, 2f),
                new Vector3(3f, 4f, -2f),
                new Vector3(0.5f, 1f, 0f)
            };

            Assert.That(
                FoodInteractionBoundsCalculator.TryCalculateFromLocalPoints(points, out var bounds),
                Is.True);
            Assert.That(bounds.min, Is.EqualTo(new Vector3(-1f, 0f, -2f)));
            Assert.That(bounds.max, Is.EqualTo(new Vector3(3f, 4f, 2f)));
        }

        [Test]
        public void Bounds_EncloseEveryPointForArbitraryShapes()
        {
            var random = new System.Random(20260828);
            var points = new Vector3[200];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new Vector3(
                    (float)random.NextDouble() * 6f - 3f,
                    (float)random.NextDouble() * 6f - 3f,
                    (float)random.NextDouble() * 6f - 3f);
            }

            var expectedMin = points[0];
            var expectedMax = points[0];
            foreach (var point in points)
            {
                expectedMin = Vector3.Min(expectedMin, point);
                expectedMax = Vector3.Max(expectedMax, point);
            }

            Assert.That(
                FoodInteractionBoundsCalculator.TryCalculateFromLocalPoints(points, out var bounds),
                Is.True);

            // Bounds は center/extents で保持されるため、境界上の点は丸め誤差ぶんだけ外れうる。
            const float tolerance = 1e-4f;
            foreach (var point in points)
            {
                Assert.That(point.x, Is.InRange(bounds.min.x - tolerance, bounds.max.x + tolerance));
                Assert.That(point.y, Is.InRange(bounds.min.y - tolerance, bounds.max.y + tolerance));
                Assert.That(point.z, Is.InRange(bounds.min.z - tolerance, bounds.max.z + tolerance));
            }

            Assert.That(bounds.min, Is.EqualTo(expectedMin).Using(new Vector3EqualityComparer(tolerance)));
            Assert.That(bounds.max, Is.EqualTo(expectedMax).Using(new Vector3EqualityComparer(tolerance)));
        }

        [Test]
        public void FlatShape_KeepsMinimumThicknessSoTheColliderStaysUsable()
        {
            var points = new[]
            {
                new Vector3(-1f, 0f, -1f),
                new Vector3(1f, 0f, 1f)
            };

            Assert.That(
                FoodInteractionBoundsCalculator.TryCalculateFromLocalPoints(points, out var bounds),
                Is.True);
            Assert.That(bounds.size.y, Is.EqualTo(FoodInteractionBoundsCalculator.MinimumLocalExtent));
            Assert.That(bounds.size.x, Is.EqualTo(2f));
        }
    }
}
