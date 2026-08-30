using System;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;
using YummyVerse.Scripts.Infrastructure;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;

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

        [Test]
        public void EndpointManager_LoadsEndpointAndTokenFromConfigStore()
        {
            var store = new ConfigStoreStub
            {
                EndpointUrl = "https://cached.example.test/v2",
                DeviceAccessToken = "cached-device-token"
            };

            var manager = new EndPointManager(store);

            Assert.That(manager.baseEndPointUrl, Is.EqualTo("https://cached.example.test/v2"));
            Assert.That(manager.DeviceAccessToken, Is.EqualTo("cached-device-token"));
            Assert.That(store.SaveCount, Is.Zero);
        }

        [Test]
        public void EndpointManager_SavesAcceptedChangesAndTokenRemoval()
        {
            var store = new ConfigStoreStub { HasCachedValue = false };
            var manager = new EndPointManager(store);

            Assert.That(manager.UpdateEndPointUrl("https://api.example.test/v2"), Is.True);
            Assert.That(manager.UpdateDeviceAccessToken("device-token"), Is.True);
            Assert.That(manager.UpdateDeviceAccessToken("invalid token"), Is.False);

            Assert.That(store.EndpointUrl, Is.EqualTo("https://api.example.test/v2"));
            Assert.That(store.DeviceAccessToken, Is.EqualTo("device-token"));
            Assert.That(store.SaveCount, Is.EqualTo(2));

            manager.ClearDeviceAccessToken();

            Assert.That(store.DeviceAccessToken, Is.Empty);
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void PersistentConfigStore_RoundTripsEndpointAndToken()
        {
            var cachePath = Path.Combine(
                Path.GetTempPath(),
                $"yummy-service-v2-config-{Guid.NewGuid():N}.json");

            try
            {
                var writer = new PersistentYummyServiceV2ConfigStore(cachePath);
                writer.Save("https://api.example.test/v2", "device-token");

                var reader = new PersistentYummyServiceV2ConfigStore(cachePath);

                Assert.That(reader.TryLoad(out var endpointUrl, out var deviceAccessToken), Is.True);
                Assert.That(endpointUrl, Is.EqualTo("https://api.example.test/v2"));
                Assert.That(deviceAccessToken, Is.EqualTo("device-token"));
            }
            finally
            {
                if (File.Exists(cachePath)) File.Delete(cachePath);
            }
        }

        private sealed class ConfigStoreStub : IYummyServiceV2ConfigStore
        {
            public bool HasCachedValue { get; set; } = true;
            public string EndpointUrl { get; set; } = string.Empty;
            public string DeviceAccessToken { get; set; } = string.Empty;
            public int SaveCount { get; private set; }

            public bool TryLoad(out string endpointUrl, out string deviceAccessToken)
            {
                endpointUrl = EndpointUrl;
                deviceAccessToken = DeviceAccessToken;
                return HasCachedValue;
            }

            public void Save(string endpointUrl, string deviceAccessToken)
            {
                EndpointUrl = endpointUrl;
                DeviceAccessToken = deviceAccessToken;
                SaveCount++;
            }
        }
    }
}
