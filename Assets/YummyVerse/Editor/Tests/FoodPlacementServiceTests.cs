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

            // 表示先が無いまま食べ物を出すと、モデルは読み込まれても画面には何も出ない。
            Assert.That(service.IsPlacementConfigured.Value, Is.False);

            service.UpdateDraftPose(new Pose(Vector3.one, Quaternion.identity));

            Assert.That(service.IsPlacementConfigured.Value, Is.True);

            service.Dispose();
        }

        [Test]
        public void PlacementBecomesConfiguredWhenSavedAnchorIsRestored()
        {
            var anchor = new GameObject("Test Anchor").transform;
            var service = new FoodPlacementService(
                new ScriptedSpatialAnchorBackend(SpatialAnchorBackendResult.Succeeded(Guid.NewGuid(), anchor), anchor),
                new SavedPlacementStore());

            service.Initialize();

            Assert.That(service.IsBusy.Value, Is.False, "復元は同期的に完了しているはず");
            Assert.That(service.IsPlacementConfigured.Value, Is.True);

            service.Dispose();
            UnityEngine.Object.DestroyImmediate(anchor.gameObject);
        }

        [Test]
        public void PlacementStaysUnconfiguredWhenSavedAnchorCannotBeRestored()
        {
            var service = new FoodPlacementService(
                new ScriptedSpatialAnchorBackend(SpatialAnchorBackendResult.Failed("anchor not found"), null),
                new SavedPlacementStore());

            service.Initialize();

            // 保存済み設定があってもAnchorをlocalizeできなければ表示先は無い。
            // ここをtrueに倒すと、案内も出ないまま食べ物だけが見えない状態になる。
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

        /// <summary>LoadAsync の結果を指定できるバックエンド。復元成功・失敗の両方を再現する。</summary>
        private sealed class ScriptedSpatialAnchorBackend : ISpatialAnchorBackend
        {
            private readonly SpatialAnchorBackendResult _loadResult;

            public ScriptedSpatialAnchorBackend(SpatialAnchorBackendResult loadResult, Transform anchorTransform)
            {
                _loadResult = loadResult;
                CurrentAnchorTransform = anchorTransform;
            }

            public Guid CurrentUuid => Guid.Empty;
            public Transform CurrentAnchorTransform { get; }

            public UniTask<SpatialAnchorBackendResult> LoadAsync(
                Guid uuid,
                CancellationToken cancellationToken) =>
                UniTask.FromResult(_loadResult);

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
