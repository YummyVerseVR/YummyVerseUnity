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

        [Test]
        public void PlacementIsUnconfiguredUntilDraftPoseIsSet()
        {
            var service = new FoodPlacementService(new StubSpatialAnchorBackend(), new EmptyPlacementStore());
            service.Initialize();

            // 未設定のまま食べ物を出すと表示先が無く、モデルは読み込まれても何も見えない。
            Assert.That(service.IsPlacementConfigured.Value, Is.False);

            service.UpdateDraftPose(new Pose(Vector3.one, Quaternion.identity));

            Assert.That(service.IsPlacementConfigured.Value, Is.True);

            service.Dispose();
        }

        [Test]
        public void PlacementStaysConfiguredWhileSavedAnchorIsRestoring()
        {
            var backend = new PendingSpatialAnchorBackend();
            var service = new FoodPlacementService(backend, new SavedPlacementStore());

            service.Initialize();

            // Anchorの復元完了を待つ間に「未設定」と判定すると、起動直後に案内が一瞬出てしまう。
            Assert.That(service.IsPlacementConfigured.Value, Is.True);

            backend.CompleteLoadWithFailure();

            // 復元に失敗したなら本当に表示先が無いので、未設定として案内する。
            Assert.That(service.IsPlacementConfigured.Value, Is.False);

            service.Dispose();
        }

        private sealed class SavedPlacementStore : IFoodPlacementStore
        {
            public bool TryLoad(out FoodPlacementData data)
            {
                data = new FoodPlacementData
                {
                    SchemaVersion = FoodPlacementData.CurrentSchemaVersion,
                    AnchorUuid = Guid.NewGuid().ToString("D"),
                    HasFoodPose = true,
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity
                };
                return true;
            }

            public void Save(FoodPlacementData data)
            {
            }
        }

        /// <summary>LoadAsync を任意のタイミングまで未完了に保ち、復元中の状態を観測できるようにする。</summary>
        private sealed class PendingSpatialAnchorBackend : ISpatialAnchorBackend
        {
            private readonly UniTaskCompletionSource<SpatialAnchorBackendResult> _load = new();

            public Guid CurrentUuid => Guid.Empty;
            public Transform CurrentAnchorTransform => null;

            public void CompleteLoadWithFailure() =>
                _load.TrySetResult(SpatialAnchorBackendResult.Failed("anchor not found"));

            public UniTask<SpatialAnchorBackendResult> LoadAsync(
                Guid uuid,
                CancellationToken cancellationToken) =>
                _load.Task;

            public UniTask<SpatialAnchorBackendResult> ReplaceAsync(
                Pose pose,
                CancellationToken cancellationToken) =>
                UniTask.FromResult(SpatialAnchorBackendResult.Failed("not used"));

            public UniTask CommitReplacementAsync() => UniTask.CompletedTask;
            public UniTask RollbackReplacementAsync() => UniTask.CompletedTask;
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
