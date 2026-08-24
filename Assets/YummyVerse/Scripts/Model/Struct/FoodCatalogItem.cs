using System;
using System.Collections.Generic;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 一覧表示用の軽量な食品情報。ModelLocation は選択されるまで読み込まない。
    /// </summary>
    public sealed class FoodCatalogItem
    {
        public FoodCatalogItem(
            string id,
            string displayName,
            string previewLocation,
            string modelLocation,
            MenuItemSource source,
            bool isAvailable = true)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            PreviewLocation = previewLocation ?? string.Empty;
            ModelLocation = modelLocation ?? string.Empty;
            Source = source;
            IsAvailable = isAvailable;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string PreviewLocation { get; }
        public string ModelLocation { get; }
        public MenuItemSource Source { get; }
        public bool IsAvailable { get; }

        public bool IsSelectable => IsAvailable
                                    && !string.IsNullOrWhiteSpace(Id)
                                    && !string.IsNullOrWhiteSpace(DisplayName)
                                    && !string.IsNullOrWhiteSpace(ModelLocation);
    }

    public sealed class FoodCatalogLoadResult
    {
        public FoodCatalogLoadResult(IReadOnlyList<FoodCatalogItem> items, string apiError = null)
        {
            Items = items ?? Array.Empty<FoodCatalogItem>();
            ApiError = apiError ?? string.Empty;
        }

        public IReadOnlyList<FoodCatalogItem> Items { get; }
        public string ApiError { get; }
    }
}
