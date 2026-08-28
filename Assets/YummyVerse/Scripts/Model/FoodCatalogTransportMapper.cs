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

                items.Add(new FoodCatalogItem(
                    $"api-v2:{dto.id}",
                    dto.display_name,
                    previewUrl,
                    modelUrl,
                    MenuItemSource.ApiV2,
                    dto.available));
            }

            return items;
        }
    }
}
