using System.Net;
using System.Threading;
using NUnit.Framework;
using YummyVerse.Scripts.Infrastructure;
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
        public void ConnectionTest_RequiresAConfiguredV2Endpoint()
        {
            var endpoint = new EndPointManager();
            var tester = new NetworkConnectionTester(endpoint);

            var result = tester.TestConnection(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [TestCase("https://api.example.test", true)]
        [TestCase("http://127.0.0.1:8010", true)]
        [TestCase("http://localhost:8010", true)]
        [TestCase("http://api.example.test", false)]
        public void EndpointValidationAllowsHttpsAndLoopbackDevelopmentHttp(string url, bool expected)
        {
            var endpoint = new EndPointManager();

            Assert.That(endpoint.UpdateEndPointUrl(url), Is.EqualTo(expected));
        }
    }
}
