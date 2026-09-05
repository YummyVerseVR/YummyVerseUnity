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

        /// <summary>
        /// 被り直すと Spatial Anchor はランタイムによって物理空間へ置き直される
        /// (= Transform が動く)。食品はそれに付いていかなければ現実に対してずれる。
        /// </summary>
        [Test]
        public void FoodFollowsAnchorWhenTheAnchorIsRelocated()
        {
            var anchor = new GameObject("Test Anchor").transform;
            anchor.SetPositionAndRotation(new Vector3(1f, 0f, 0f), Quaternion.identity);
            var service = new FoodPlacementService(
                new ScriptedSpatialAnchorBackend(SpatialAnchorBackendResult.Failed("not used"), anchor),
                new EmptyPlacementStore());

            var draft = new Pose(new Vector3(1.5f, 0.9f, 0.2f), Quaternion.Euler(0f, 45f, 0f));
            service.UpdateDraftPose(draft);
            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);

            var foodTransform = service.FoodTransform.Value;
            var localOffset = draft.position - anchor.position;

            // 再センタリング相当。アンカーが動いた分だけ食品も動いていること。
            anchor.SetPositionAndRotation(new Vector3(-2f, 0.3f, 4f), Quaternion.Euler(0f, 90f, 0f));

            var expected = anchor.position + anchor.rotation * localOffset;
            Assert.That(Vector3.Distance(foodTransform.position, expected), Is.LessThan(0.0001f));

            service.Dispose();
            UnityEngine.Object.DestroyImmediate(anchor.gameObject);
        }

        /// <summary>
        /// 食品を出すたびに下書きが再適用される。ここでワールド座標を焼き直すと、
        /// 被り直しをまたいだ2皿目から現実に対してずれる (今回の不具合)。
        /// </summary>
        [Test]
        public void ReactivatingDraftAfterRelocationKeepsTheAnchorRelativePlacement()
        {
            var anchor = new GameObject("Test Anchor").transform;
            var service = new FoodPlacementService(
                new ScriptedSpatialAnchorBackend(SpatialAnchorBackendResult.Failed("not used"), anchor),
                new EmptyPlacementStore());

            var draft = new Pose(new Vector3(0.3f, -0.1f, 0.7f), Quaternion.Euler(0f, 20f, 0f));
            service.UpdateDraftPose(draft);
            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);

            anchor.SetPositionAndRotation(new Vector3(3f, 1f, -1f), Quaternion.Euler(0f, 180f, 0f));

            // 2皿目。下書きはアンカー基準で保たれているはず。
            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);
            var foodTransform = service.FoodTransform.Value;

            var expected = anchor.position + anchor.rotation * draft.position;
            Assert.That(Vector3.Distance(foodTransform.position, expected), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(foodTransform.rotation, anchor.rotation * draft.rotation), Is.LessThan(0.01f));

            service.Dispose();
            UnityEngine.Object.DestroyImmediate(anchor.gameObject);
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
