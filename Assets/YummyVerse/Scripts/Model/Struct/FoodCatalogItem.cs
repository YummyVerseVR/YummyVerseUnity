using System;
using System.Collections.Generic;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 一覧表示用の軽量な食品情報。ModelLocation / AudioLocation は選択されるまで読み込まない。
    /// </summary>
    public sealed class FoodCatalogItem
    {
        public FoodCatalogItem(
            string id,
            string displayName,
            string previewLocation,
            string modelLocation,
            string audioLocation,
            MenuItemSource source,
            bool isAvailable = true)
            : this(
                id,
                displayName,
                previewLocation,
                modelLocation,
                audioLocation,
                source,
                isAvailable,
                string.Empty,
                string.Empty,
                string.Empty)
        {
        }

        /// <summary>
        /// Creates a catalog item while retaining the opaque Device order/artifact
        /// identity that produced its model and optional chewing sound.  Identity is
        /// metadata only; URL construction remains centralized in YummyServiceV2Url.
        /// </summary>
        public FoodCatalogItem(
            string id,
            string displayName,
            string previewLocation,
            string modelLocation,
            string audioLocation,
            MenuItemSource source,
            bool isAvailable,
            string orderId,
            string modelArtifactId,
            string audioArtifactId)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            PreviewLocation = previewLocation ?? string.Empty;
            ModelLocation = modelLocation ?? string.Empty;
            AudioLocation = audioLocation ?? string.Empty;
            Source = source;
            IsAvailable = isAvailable;
            OrderId = orderId ?? string.Empty;
            ModelArtifactId = modelArtifactId ?? string.Empty;
            AudioArtifactId = audioArtifactId ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string PreviewLocation { get; }
        public string ModelLocation { get; }

        /// <summary>
        /// 咀嚼音の場所。ローカル食品はフォルダ内の audio ファイル、API v2 は音声のURL。
        /// 用意されていない食品もあるため、空でも選択可能とする(既定の咀嚼音へフォールバックする)。
        /// </summary>
        public string AudioLocation { get; }

        /// <summary>Opaque v2 order identity; empty for local items.</summary>
        public string OrderId { get; }

        /// <summary>Selected GLB artifact identity; empty when not downloadable.</summary>
        public string ModelArtifactId { get; }

        /// <summary>Selected WAV artifact identity; empty when no audio is downloadable.</summary>
        public string AudioArtifactId { get; }

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
