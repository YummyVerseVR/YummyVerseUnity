using NUnit.Framework;
using YummyVerse.Scripts.Model;

namespace YummyVerse.Editor.Tests
{
    /// <summary>プロトコル仕様書 §10 のリクエストID発番。</summary>
    public class ChewingRequestIdSequenceTests
    {
        [Test]
        public void FirstIdIsOne_AndIncrements()
        {
            var sequence = new ChewingRequestIdSequence();

            Assert.That(sequence.Next(), Is.EqualTo(1u));
            Assert.That(sequence.Next(), Is.EqualTo(2u));
        }

        [Test]
        public void WrapsFromMaxValueToOne_SkippingTheReservedZero()
        {
            var sequence = new ChewingRequestIdSequence();
            for (var i = 0; i < 3; i++) sequence.Next();

            // uint.MaxValue まで回すのは非現実的なので、折り返しの直前へ寄せてから確認する。
            var field = typeof(ChewingRequestIdSequence)
                .GetField("_current", System.Reflection.BindingFlags.Instance
                                      | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(sequence, uint.MaxValue);

            Assert.That(sequence.Next(), Is.EqualTo(1u));
        }
    }
}
