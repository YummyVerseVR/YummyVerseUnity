using System.Net;
using System.Threading;
using NUnit.Framework;
using YummyVerse.Scripts.Model;

namespace YummyVerse.Editor.Tests
{
    public class NetworkContractGateTests
    {
        [Test]
        public void EndpointManager_HasNoLegacyDefaultAndRequiresHttps()
        {
            var manager = new EndPointManager();

            Assert.That(manager.baseEndPointUrl, Is.Empty);
            Assert.That(manager.UpdateEndPointUrl("http://example.test/v2"), Is.False);
            Assert.That(manager.UpdateEndPointUrl("https://example.test/v2"), Is.True);
        }

        [Test]
        public void ConnectionTest_FailsClosedWithoutSendingARequest()
        {
            var tester = new NetworkConnectionTester();

            var result = tester.TestConnection(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }
    }
}
