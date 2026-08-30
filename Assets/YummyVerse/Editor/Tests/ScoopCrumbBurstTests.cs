using NUnit.Framework;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    public class ScoopCrumbBurstTests
    {
        [Test]
        public void Resolve_EmitsFromTheContactPointOnTheFoodSurface()
        {
            var contactPoint = new Vector3(0.1f, 0.9f, -0.3f);

            var burst = ScoopCrumbBurst.Resolve(contactPoint, contactPoint + Vector3.right * 0.05f, Vector3.up);

            Assert.That(burst.Position, Is.EqualTo(contactPoint));
        }

        [Test]
        public void Resolve_PointsAwayFromTheFoodAndLeansUpward()
        {
            var contactPoint = Vector3.zero;

            var burst = ScoopCrumbBurst.Resolve(contactPoint, new Vector3(1f, 0f, 0f), Vector3.up);

            Assert.That(burst.Direction.magnitude, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(burst.Direction.x, Is.GreaterThan(0f), "手元 (すくい体積) の方向へ飛ぶ");
            Assert.That(burst.Direction.y, Is.GreaterThan(0f), "皿へ落ちるだけにならないよう上向きが混ざる");
            Assert.That(burst.Direction.x, Is.GreaterThan(burst.Direction.y), "上向きは補正であって主方向ではない");
        }

        [Test]
        public void Resolve_FallsBackWhenTheProbeCentreIsBuriedInTheFood()
        {
            // めり込んでいる間、Collider.ClosestPoint はすくい体積の中心そのものを返す。
            var contactPoint = new Vector3(0.2f, 1f, 0.4f);

            var burst = ScoopCrumbBurst.Resolve(contactPoint, contactPoint, Vector3.up);

            Assert.That(burst.Position, Is.EqualTo(contactPoint));
            Assert.That(burst.Direction, Is.EqualTo(Vector3.up));
        }

        [Test]
        public void Resolve_UsesWorldUpWhenTheFallbackDirectionIsDegenerate()
        {
            var burst = ScoopCrumbBurst.Resolve(Vector3.zero, Vector3.zero, Vector3.zero);

            Assert.That(burst.Direction, Is.EqualTo(Vector3.up));
        }

        [Test]
        public void Resolve_StaysNormalisedWhenTheOutwardDirectionOpposesTheFallback()
        {
            // 真下へ向かってすくった場合でも、向きが 0 ベクトルに潰れない。
            var burst = ScoopCrumbBurst.Resolve(Vector3.zero, Vector3.down, Vector3.up);

            Assert.That(burst.Direction.magnitude, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(burst.Direction.y, Is.LessThan(0f));
        }
    }
}
