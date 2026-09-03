using NUnit.Framework;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    public sealed class YummyServiceV2DeviceMapperTests
    {
        [Test]
        public void DeviceProjectionMapsOnlyDownloadableSelectedArtifacts()
        {
            var response = new DeviceOrderListResponseDto
            {
                items = new[]
                {
                    new CustomerOrderStatusDto
                    {
                        order_id = "order/ready",
                        food_name = "寿司",
                        state = "COMPLETED",
                        analysis = new CustomerStageStatusDto { state = "COMPLETED" },
                        generated_image = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "image-ready"
                        },
                        glb = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "glb-ready"
                        },
                        wav = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "wav-ready"
                        },
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    },
                    new CustomerOrderStatusDto
                    {
                        order_id = "order/pending",
                        food_name = "ラーメン",
                        state = "PROCESSING",
                        analysis = new CustomerStageStatusDto { state = "PROCESSING" },
                        generated_image = new CustomerOutputStatusDto
                        {
                            state = "PROCESSING",
                            downloadable = false
                        },
                        glb = new CustomerOutputStatusDto
                        {
                            state = "PROCESSING",
                            downloadable = false,
                            artifact_id = "should-not-be-used"
                        },
                        wav = new CustomerOutputStatusDto
                        {
                            state = "PENDING",
                            downloadable = false
                        },
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    }
                }
            };

            var items = FoodCatalogTransportMapper.ToCatalogItems(
                response,
                "https://example.test/v2");

            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].OrderId, Is.EqualTo("order/ready"));
            Assert.That(items[0].ModelArtifactId, Is.EqualTo("glb-ready"));
            Assert.That(items[0].ModelLocation, Is.EqualTo(
                "https://example.test/v2/devices/unity/orders/order%2Fready/artifacts/glb-ready/download"));
            Assert.That(items[0].AudioArtifactId, Is.EqualTo("wav-ready"));
            Assert.That(items[0].PreviewLocation, Is.EqualTo(
                "https://example.test/v2/devices/unity/orders/order%2Fready/artifacts/image-ready/download"));
            Assert.That(items[0].IsSelectable, Is.True);

            Assert.That(items[1].ModelArtifactId, Is.Empty);
            Assert.That(items[1].ModelLocation, Is.Empty);
            Assert.That(items[1].PreviewLocation, Is.Empty);
            Assert.That(items[1].IsSelectable, Is.False);
        }

        [Test]
        public void DeviceProjectionDoesNotExposeNonCanonicalGeneratedImageArtifacts()
        {
            var response = new DeviceOrderListResponseDto
            {
                items = new[]
                {
                    new CustomerOrderStatusDto
                    {
                        order_id = "order-processing",
                        food_name = "寿司",
                        state = "PROCESSING",
                        analysis = new CustomerStageStatusDto { state = "PROCESSING" },
                        generated_image = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "should-not-be-used"
                        },
                        glb = new CustomerOutputStatusDto { state = "PROCESSING", downloadable = false },
                        wav = new CustomerOutputStatusDto { state = "PROCESSING", downloadable = false },
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    },
                    new CustomerOrderStatusDto
                    {
                        order_id = "order-image-pending",
                        food_name = "ラーメン",
                        state = "COMPLETED",
                        analysis = new CustomerStageStatusDto { state = "COMPLETED" },
                        generated_image = new CustomerOutputStatusDto
                        {
                            state = "PROCESSING",
                            downloadable = true,
                            artifact_id = "should-not-be-used"
                        },
                        glb = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "glb-ready"
                        },
                        wav = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "wav-ready"
                        },
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    },
                    new CustomerOrderStatusDto
                    {
                        order_id = "order-image-hidden",
                        food_name = "ケーキ",
                        state = "COMPLETED",
                        analysis = new CustomerStageStatusDto { state = "COMPLETED" },
                        generated_image = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = false,
                            artifact_id = "should-not-be-used"
                        },
                        glb = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "glb-ready"
                        },
                        wav = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "wav-ready"
                        },
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    }
                }
            };

            var items = FoodCatalogTransportMapper.ToCatalogItems(
                response,
                "https://example.test/v2");

            Assert.That(items, Has.Count.EqualTo(3));
            Assert.That(items[0].PreviewLocation, Is.Empty);
            Assert.That(items[1].PreviewLocation, Is.Empty);
            Assert.That(items[2].PreviewLocation, Is.Empty);
        }

        [Test]
        public void DeviceProjectionRejectsMissingGeneratedImageProjection()
        {
            var response = new DeviceOrderListResponseDto
            {
                items = new[]
                {
                    new CustomerOrderStatusDto
                    {
                        order_id = "missing-generated-image",
                        food_name = "寿司",
                        state = "COMPLETED",
                        analysis = new CustomerStageStatusDto { state = "COMPLETED" },
                        generated_image = null,
                        glb = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "glb"
                        },
                        wav = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "wav"
                        },
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    }
                }
            };

            Assert.That(
                FoodCatalogTransportMapper.ToCatalogItems(response, "https://example.test/v2"),
                Is.Empty);
        }

        [Test]
        public void DeviceProjectionRejectsMissingRequiredFieldsAndUnknownEnums()
        {
            var response = new DeviceOrderListResponseDto
            {
                items = new[]
                {
                    new CustomerOrderStatusDto
                    {
                        order_id = "missing-wav",
                        food_name = "寿司",
                        state = "COMPLETED",
                        analysis = new CustomerStageStatusDto { state = "COMPLETED" },
                        generated_image = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "image"
                        },
                        glb = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "glb"
                        },
                        wav = null,
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    },
                    new CustomerOrderStatusDto
                    {
                        order_id = "unknown-state",
                        food_name = "ケーキ",
                        state = "NEW_STATE",
                        analysis = new CustomerStageStatusDto { state = "COMPLETED" },
                        generated_image = new CustomerOutputStatusDto
                        {
                            state = "COMPLETED",
                            downloadable = true,
                            artifact_id = "image"
                        },
                        glb = new CustomerOutputStatusDto { state = "COMPLETED", downloadable = true, artifact_id = "glb" },
                        wav = new CustomerOutputStatusDto { state = "COMPLETED", downloadable = true, artifact_id = "wav" },
                        created_at = "2026-08-30T01:00:00Z",
                        updated_at = "2026-08-30T01:01:00Z"
                    }
                }
            };

            Assert.That(
                FoodCatalogTransportMapper.ToCatalogItems(response, "https://example.test"),
                Is.Empty);
        }

        [Test]
        public void EndpointManagerAcceptsRuntimeTokenAndRejectsWhitespace()
        {
            var manager = new EndPointManager();
            Assert.That(manager.DeviceAccessToken, Is.Empty);
            Assert.That(manager.UpdateDeviceAccessToken("  device-token-1  "), Is.True);
            Assert.That(manager.DeviceAccessToken, Is.EqualTo("device-token-1"));
            Assert.That(manager.UpdateDeviceAccessToken("bad token"), Is.False);
            Assert.That(manager.DeviceAccessToken, Is.EqualTo("device-token-1"));
            Assert.That(manager.UpdateDeviceAccessToken(""), Is.False);
            manager.ClearDeviceAccessToken();
            Assert.That(manager.DeviceAccessToken, Is.Empty);
        }
    }
}
