using System;
using System.Collections.Generic;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// Converts v2 wire DTOs into application catalog records. No HTTP or Unity object
    /// lifetime belongs in this mapping boundary.
    /// </summary>
    public static class FoodCatalogTransportMapper
    {
        /// <summary>
        /// Maps the sanitized Unity Device order projection to menu records.
        ///
        /// Device status intentionally has no preview URL or artifact checksum.
        /// Consequently a generated item receives an artifact download URL only
        /// when the server marks that selected output as downloadable.  Incomplete
        /// orders remain visible as disabled cards, but no guessed artifact ID/URL is
        /// ever produced.
        /// </summary>
        public static IReadOnlyList<FoodCatalogItem> ToCatalogItems(
            DeviceOrderListResponseDto response,
            string configuredBaseUrl)
        {
            var items = new List<FoodCatalogItem>();
            if (response?.items == null) return items;

            foreach (var dto in response.items)
            {
                if (!TryMapDeviceOrder(dto, configuredBaseUrl, out var item)) continue;
                items.Add(item);
            }

            return items;
        }

        public static IReadOnlyList<FoodCatalogItem> ToCatalogItems(
            MenuResponseDto response,
            string configuredBaseUrl)
        {
            var items = new List<FoodCatalogItem>();
            if (response?.items == null) return items;

            foreach (var dto in response.items)
            {
                if (dto == null
                    || string.IsNullOrWhiteSpace(dto.id)
                    || string.IsNullOrWhiteSpace(dto.display_name))
                {
                    continue;
                }

                YummyServiceV2Url.TryResolveLocation(
                    configuredBaseUrl,
                    dto.thumbnail_url,
                    out var previewUrl);
                YummyServiceV2Url.TryResolveLocation(
                    configuredBaseUrl,
                    dto.sample_glb_url,
                    out var modelUrl);
                // 咀嚼音サンプル。contract 上 string | null なので、無い item は空のままにする。
                YummyServiceV2Url.TryResolveLocation(
                    configuredBaseUrl,
                    dto.sample_wav_url,
                    out var audioUrl);

                items.Add(new FoodCatalogItem(
                    $"api-v2:{dto.id}",
                    dto.display_name,
                    previewUrl,
                    modelUrl,
                    audioUrl,
                    MenuItemSource.ApiV2,
                    dto.available));
            }

            return items;
        }

        private static bool TryMapDeviceOrder(
            CustomerOrderStatusDto dto,
            string configuredBaseUrl,
            out FoodCatalogItem item)
        {
            item = null;
            if (dto == null
                || string.IsNullOrWhiteSpace(dto.order_id)
                || string.IsNullOrWhiteSpace(dto.food_name)
                || dto.food_name.Length > 100
                || dto.analysis == null
                || dto.glb == null
                || dto.wav == null
                || string.IsNullOrWhiteSpace(dto.created_at)
                || string.IsNullOrWhiteSpace(dto.updated_at)
                || !DateTimeOffset.TryParse(dto.created_at, out _)
                || !DateTimeOffset.TryParse(dto.updated_at, out _))
            {
                return false;
            }

            if (!YummyServiceV2ContractGuard.TryParseOrderState(dto.state, out var orderState))
            {
                return false;
            }

            // All three projections are required by CustomerOrderStatus.  Unknown
            // stage values are never converted to a selectable (or an optimistic
            // disabled) menu item.
            if (!YummyServiceV2ContractGuard.TryParseStageState(
                    dto.analysis.state, out _)
                || !YummyServiceV2ContractGuard.TryParseStageState(
                    dto.glb.state, out var glbState)
                || !YummyServiceV2ContractGuard.TryParseStageState(
                    dto.wav.state, out var wavState))
            {
                return false;
            }

            // The Device projection only promises selected/verified/completed output
            // through downloadable=true and a present artifact_id.  Never derive an
            // ID from order_id or a missing URL.
            var glbDownloadable = orderState == OrderState.Completed
                                  && dto.glb.downloadable
                                  && !string.IsNullOrWhiteSpace(dto.glb.artifact_id)
                                  && glbState == StageState.Completed;

            var modelArtifactId = glbDownloadable ? dto.glb.artifact_id : string.Empty;
            var modelUrl = string.Empty;
            if (glbDownloadable)
            {
                YummyServiceV2Url.TryBuildUnityDeviceArtifactDownloadUrl(
                    configuredBaseUrl,
                    dto.order_id,
                    modelArtifactId,
                    out modelUrl);
            }

            var audioArtifactId = string.Empty;
            var audioUrl = string.Empty;
            if (orderState == OrderState.Completed
                && dto.wav != null
                && dto.wav.downloadable
                && !string.IsNullOrWhiteSpace(dto.wav.artifact_id)
                && wavState == StageState.Completed)
            {
                audioArtifactId = dto.wav.artifact_id;
                YummyServiceV2Url.TryBuildUnityDeviceArtifactDownloadUrl(
                    configuredBaseUrl,
                    dto.order_id,
                    audioArtifactId,
                    out audioUrl);
            }

            item = new FoodCatalogItem(
                $"api-v2:order:{dto.order_id}",
                dto.food_name,
                string.Empty,
                modelUrl,
                audioUrl,
                MenuItemSource.ApiV2,
                glbDownloadable,
                dto.order_id,
                modelArtifactId,
                audioArtifactId);
            return true;
        }
    }
}
