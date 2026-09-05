using NUnit.Framework;
using YummyVerse.Scripts.Model;

namespace YummyVerse.Editor.Tests
{
    /// <summary>
    /// PCVR (Quest Link) の着脱で描画負荷を落とす/戻すの判断。
    /// 「落とすのは即座、上げるのは落ち着いてから」が守られていることの裏付け。
    /// </summary>
    public class XrSessionAvailabilityTrackerTests
    {
        private const float Settle = 1f;
        private const int SettleFrames = 3;

        private static XrSessionAvailabilityTracker NewTracker() => new(Settle, SettleFrames);

        [Test]
        public void StartsAvailable_SoNonVrRunsAreNeverThrottled()
        {
            Assert.That(NewTracker().IsAvailable, Is.True);
        }

        [Test]
        public void BecomesUnavailable_OnTheFirstUnusableObservation()
        {
            var tracker = NewTracker();

            Assert.That(tracker.Observe(false, 0f), Is.True, "状態変化が報告されること");
            Assert.That(tracker.IsAvailable, Is.False);
        }

        [Test]
        public void StaysUnavailable_UntilEnoughTimeHasPassed()
        {
            var tracker = NewTracker();
            tracker.Observe(false, 0f);

            // フレーム数は満たしているが、経過時間が足りない。
            // 高フレームレートで被り直した直後がこれに当たる。
            for (var i = 0; i < 10; i++) tracker.Observe(true, 0.01f * i);

            Assert.That(tracker.IsAvailable, Is.False);
        }

        [Test]
        public void StaysUnavailable_UntilEnoughFramesHavePassed()
        {
            var tracker = NewTracker();
            tracker.Observe(false, 0f);

            // 経過時間は満たしているが、フレーム数が足りない。
            // 復帰直後にフレームが詰まっているときがこれに当たる。
            tracker.Observe(true, 100f);
            tracker.Observe(true, 200f);
            Assert.That(tracker.IsAvailable, Is.False);

            tracker.Observe(true, 201f);
            Assert.That(tracker.IsAvailable, Is.True);
        }

        [Test]
        public void BecomesAvailable_OnceBothTimeAndFramesAreSatisfied()
        {
            var tracker = NewTracker();
            tracker.Observe(false, 0f);

            tracker.Observe(true, 1f);
            tracker.Observe(true, 2f);
            Assert.That(tracker.IsAvailable, Is.False, "3フレーム目までは上がらない");

            Assert.That(tracker.Observe(true, 2.1f), Is.True);
            Assert.That(tracker.IsAvailable, Is.True);
        }

        [Test]
        public void SettleWindow_RestartsWhenTheRuntimeDropsAgain()
        {
            var tracker = NewTracker();
            tracker.Observe(false, 0f);

            tracker.Observe(true, 1f);
            tracker.Observe(true, 2f);

            // あと1フレームで復帰、というところで落ちたらやり直し。
            tracker.Observe(false, 2.5f);

            tracker.Observe(true, 3f);
            tracker.Observe(true, 3.1f);
            tracker.Observe(true, 3.2f);
            Assert.That(tracker.IsAvailable, Is.False, "時間が足りないので上げてはいけない");

            tracker.Observe(true, 4.1f);
            Assert.That(tracker.IsAvailable, Is.True);
        }

        [Test]
        public void NotifyLost_DropsImmediatelyBetweenObservations()
        {
            var tracker = NewTracker();

            Assert.That(tracker.NotifyLost(), Is.True);
            Assert.That(tracker.IsAvailable, Is.False);
        }

        [Test]
        public void NotifyLost_ForcesTheSettleWindow_EvenWhenPollingNeverSawTheDrop()
        {
            var tracker = NewTracker();

            // 着脱がフレームの合間に完了し、ポーリングは一度も false を見ないケース。
            // イベントで落としておかないと、作り直しの最中に仕事を積んでしまう。
            tracker.NotifyLost();

            tracker.Observe(true, 0.1f);
            Assert.That(tracker.IsAvailable, Is.False);

            tracker.Observe(true, 1.1f);
            tracker.Observe(true, 1.2f);
            Assert.That(tracker.IsAvailable, Is.True);
        }

        [Test]
        public void ReportsNoChange_WhileTheStateIsSteady()
        {
            var tracker = NewTracker();

            Assert.That(tracker.Observe(true, 0f), Is.False);
            Assert.That(tracker.Observe(true, 1f), Is.False);

            tracker.Observe(false, 2f);
            Assert.That(tracker.Observe(false, 3f), Is.False, "落ちたままなら報告し直さない");
            Assert.That(tracker.NotifyLost(), Is.False);
        }
    }
}
