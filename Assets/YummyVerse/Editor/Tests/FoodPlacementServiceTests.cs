using NUnit.Framework;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    public class FoodPlacementServiceTests
    {
        private const string FrameKind = "stage";

        [Test]
        public void ActivatingDraftWithoutAReferenceFrameUsesTheWorldPose()
        {
            // 基準がまだ立っていない間の暫定表示。ここは着脱でずれる前提の繋ぎでしかない。
            var frame = new StubReferenceFrame(null);
            var service = new FoodPlacementService(frame, new EmptyPlacementStore());
            var pose = new Pose(new Vector3(1.25f, 0.8f, -2.5f), Quaternion.Euler(15f, 120f, 35f));

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
            var service = new FoodPlacementService(new StubReferenceFrame(null), new EmptyPlacementStore());

            Assert.That(service.TryActivateDraftPoseForFood(), Is.False);

            service.Dispose();
        }

        [Test]
        public void PlacementIsUnconfiguredUntilDraftPoseIsSet()
        {
            var service = new FoodPlacementService(new StubReferenceFrame(null), new EmptyPlacementStore());
            service.Initialize();

            // 表示先が無いまま食べ物を出すと、モデルは読み込まれても画面には何も出ない。
            Assert.That(service.IsPlacementConfigured.Value, Is.False);

            service.UpdateDraftPose(new Pose(Vector3.one, Quaternion.identity));

            Assert.That(service.IsPlacementConfigured.Value, Is.True);

            service.Dispose();
        }

        /// <summary>
        /// 被り直すとトラッキング原点が張り直され、部屋基準の Transform はそれを打ち消すように動く。
        /// 食品がその子で居続けている限り、現実の同じ場所に留まる。
        /// </summary>
        [Test]
        public void FoodFollowsTheReferenceFrameWhenItMoves()
        {
            var frameTransform = new GameObject("Test Room Frame").transform;
            frameTransform.SetPositionAndRotation(new Vector3(1f, 0f, 0f), Quaternion.identity);
            var service = new FoodPlacementService(
                new StubReferenceFrame(frameTransform),
                new EmptyPlacementStore());

            var draft = new Pose(new Vector3(1.5f, 0.9f, 0.2f), Quaternion.Euler(0f, 45f, 0f));
            service.UpdateDraftPose(draft);
            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);

            var foodTransform = service.FoodTransform.Value;
            var localOffset = draft.position - frameTransform.position;

            // 再センタリング相当。基準が動いた分だけ食品も動いていること。
            frameTransform.SetPositionAndRotation(new Vector3(-2f, 0.3f, 4f), Quaternion.Euler(0f, 90f, 0f));

            var expected = frameTransform.position + frameTransform.rotation * localOffset;
            Assert.That(Vector3.Distance(foodTransform.position, expected), Is.LessThan(0.0001f));

            service.Dispose();
            UnityEngine.Object.DestroyImmediate(frameTransform.gameObject);
        }

        /// <summary>
        /// 食品を出すたびに下書きが再適用される。ここでワールド座標を焼き直すと、
        /// 被り直しをまたいだ2皿目から現実に対してずれる。
        /// </summary>
        [Test]
        public void ReactivatingDraftAfterTheFrameMovedKeepsTheRelativePlacement()
        {
            var frameTransform = new GameObject("Test Room Frame").transform;
            var service = new FoodPlacementService(
                new StubReferenceFrame(frameTransform),
                new EmptyPlacementStore());

            var draft = new Pose(new Vector3(0.3f, -0.1f, 0.7f), Quaternion.Euler(0f, 20f, 0f));
            service.UpdateDraftPose(draft);
            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);

            frameTransform.SetPositionAndRotation(new Vector3(3f, 1f, -1f), Quaternion.Euler(0f, 180f, 0f));

            // 2皿目。下書きは基準フレーム基準で保たれているはず。
            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);
            var foodTransform = service.FoodTransform.Value;

            var expected = frameTransform.position + frameTransform.rotation * draft.position;
            Assert.That(Vector3.Distance(foodTransform.position, expected), Is.LessThan(0.0001f));
            Assert.That(
                Quaternion.Angle(foodTransform.rotation, frameTransform.rotation * draft.rotation),
                Is.LessThan(0.01f));

            service.Dispose();
            UnityEngine.Object.DestroyImmediate(frameTransform.gameObject);
        }

        /// <summary>
        /// 基準が後から立ち上がった場合、暫定のワールド姿勢はその時点で基準基準へ移り、
        /// 以後は基準に追従しなければならない。
        /// </summary>
        [Test]
        public void PlacementRebasesOnceTheReferenceFrameAppears()
        {
            var frame = new StubReferenceFrame(null);
            var service = new FoodPlacementService(frame, new EmptyPlacementStore());

            var draft = new Pose(new Vector3(0.5f, 1f, 2f), Quaternion.identity);
            service.UpdateDraftPose(draft);
            Assert.That(service.TryActivateDraftPoseForFood(), Is.True);

            var frameTransform = new GameObject("Test Room Frame").transform;
            frame.Current = frameTransform;
            service.Tick();

            var foodTransform = service.FoodTransform.Value;
            Assert.That(ReferenceEquals(foodTransform.parent, frameTransform), Is.True, "基準の子になっていること");
            Assert.That(Vector3.Distance(foodTransform.position, draft.position), Is.LessThan(0.0001f));

            frameTransform.position = new Vector3(10f, 0f, 0f);
            Assert.That(
                Vector3.Distance(foodTransform.position, draft.position + new Vector3(10f, 0f, 0f)),
                Is.LessThan(0.0001f),
                "基準に移った後は基準に追従すること");

            service.Dispose();
            UnityEngine.Object.DestroyImmediate(frameTransform.gameObject);
        }

        /// <summary>
        /// 別の基準で測った保存値を今の基準に当てはめると、現実と無関係な場所に食品が出る。
        /// 読み捨てて設定し直させるのが正しい。
        /// </summary>
        [Test]
        public void SavedPlacementMeasuredAgainstAnotherFrameIsRejected()
        {
            var data = new FoodPlacementData
            {
                SchemaVersion = FoodPlacementData.CurrentSchemaVersion,
                ReferenceFrame = "some-other-frame",
                HasFoodPose = true,
                LocalPosition = Vector3.zero,
                LocalRotation = Quaternion.identity
            };

            Assert.That(data.IsValid(), Is.True);
            Assert.That(data.MatchesFrame(FrameKind), Is.False);
        }

        [Test]
        public void PlacementSavedByAnOlderSchemaIsRejected()
        {
            // v1 は Spatial Anchor 基準。今の基準では意味を持たない。
            var data = new FoodPlacementData
            {
                SchemaVersion = 1,
                ReferenceFrame = FrameKind,
                HasFoodPose = true,
                LocalPosition = Vector3.zero,
                LocalRotation = Quaternion.identity
            };

            Assert.That(data.IsValid(), Is.False);
        }

        private sealed class StubReferenceFrame : IPlacementReferenceFrame
        {
            private readonly ReactiveProperty<bool> _isReady = new(false);

            public StubReferenceFrame(Transform current)
            {
                Current = current;
            }

            public Transform Current { get; set; }
            public ReadOnlyReactiveProperty<bool> IsReady => _isReady;
            public string Kind => FrameKind;
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
    }
}
