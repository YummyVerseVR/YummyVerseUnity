using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    public class FoodPlacementServiceTests
    {
        [Test]
        public void ActivatingDraftUsesPlacementModelWorldPositionAndRotation()
        {
            var service = new FoodPlacementService(new StubSpatialAnchorBackend(), new EmptyPlacementStore());
            var pose = new Pose(
                new Vector3(1.25f, 0.8f, -2.5f),
                Quaternion.Euler(15f, 120f, 35f));

            service.UpdateDraftPose(pose);

            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);
            var foodTransform = service.FoodTransform.Value;
            Assert.That(foodTransform, Is.Not.Null);
            Assert.That(Vector3.Distance(foodTransform.position, pose.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(foodTransform.rotation, pose.rotation), Is.LessThan(0.0001f));

            service.Dispose();
        }

        [Test]
        public void ActivatingWithoutDraftOrSavedPlacementReturnsFalse()
        {
            var service = new FoodPlacementService(new StubSpatialAnchorBackend(), new EmptyPlacementStore());

            Assert.That(service.TryActivateDraftPoseForFood(), Is.False);

            service.Dispose();
        }

        private sealed class EmptyPlacementStore : IFoodPlacementStore
        {
            public bool TryLoad(out FoodPlacementData data)
            {
                data = default;
                return false;
            }

            public void Save(FoodPlacementData data)
            {
            }
        }

        private sealed class StubSpatialAnchorBackend : ISpatialAnchorBackend
        {
            public Guid CurrentUuid => Guid.Empty;
            public Transform CurrentAnchorTransform => null;

            public UniTask<SpatialAnchorBackendResult> LoadAsync(
                Guid uuid,
                CancellationToken cancellationToken) =>
                UniTask.FromResult(SpatialAnchorBackendResult.Failed("not used"));

            public UniTask<SpatialAnchorBackendResult> ReplaceAsync(
                Pose pose,
                CancellationToken cancellationToken) =>
                UniTask.FromResult(SpatialAnchorBackendResult.Failed("not used"));

            public UniTask CommitReplacementAsync() => UniTask.CompletedTask;
            public UniTask RollbackReplacementAsync() => UniTask.CompletedTask;
        }
    }
}
