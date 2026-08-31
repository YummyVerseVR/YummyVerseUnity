using NUnit.Framework;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    public class ScoopContactDetectorTests
    {
        private const float ReleaseMargin = 0.02f;
        private const float Cooldown = 0.5f;

        private static ScoopContactDetector Create() => new(ReleaseMargin, Cooldown);

        [Test]
        public void Contact_RaisesExactlyOnceWhileStayingInside()
        {
            var detector = Create();

            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0f), Is.True);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.02f, 0.02f), Is.False);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.03f, 0.04f), Is.False);
        }

        [Test]
        public void NoContact_DoesNotRaise()
        {
            var detector = Create();

            Assert.That(detector.TryRegisterContact(ScoopHand.Right, 0.05f, 0f), Is.False);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, 0.2f, 0.5f), Is.False);
        }

        [Test]
        public void ReEntry_RequiresLeavingBeyondReleaseMargin()
        {
            var detector = Create();
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0f), Is.True);

            // 境界付近で出入りしても、releaseMargin を超えるまでは接触継続とみなす。
            detector.TryRegisterContact(ScoopHand.Right, 0.01f, 1f);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 1.1f), Is.False);

            detector.TryRegisterContact(ScoopHand.Right, 0.05f, 2f);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 2.1f), Is.True);
        }

        [Test]
        public void ReEntry_WithinCooldown_IsNotCounted()
        {
            var detector = Create();
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0f), Is.True);

            detector.TryRegisterContact(ScoopHand.Right, 0.05f, 0.1f);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0.2f), Is.False);

            detector.TryRegisterContact(ScoopHand.Right, 0.05f, 0.3f);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0.9f), Is.True);
        }

        [Test]
        public void HandsAreTrackedIndependently()
        {
            var detector = Create();

            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0f), Is.True);
            Assert.That(detector.TryRegisterContact(ScoopHand.Left, -0.01f, 0f), Is.True);
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0.01f), Is.False);
        }

        [Test]
        public void Reset_ClearsContactState()
        {
            var detector = Create();
            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0f), Is.True);

            detector.Reset();

            Assert.That(detector.TryRegisterContact(ScoopHand.Right, -0.01f, 0.01f), Is.True);
        }
    }
}
