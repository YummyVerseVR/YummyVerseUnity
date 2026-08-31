using System;
using System.Collections.Generic;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// Result from one catalog source. A source failure is non-fatal because the other
    /// source can still provide selectable foods.
    /// </summary>
    public sealed class FoodCatalogSourceResult
    {
        public FoodCatalogSourceResult(IReadOnlyList<FoodCatalogItem> items, string error = null)
        {
            Items = items ?? Array.Empty<FoodCatalogItem>();
            Error = error ?? string.Empty;
        }

        public IReadOnlyList<FoodCatalogItem> Items { get; }
        public string Error { get; }
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        public static FoodCatalogSourceResult Empty(string error = null) =>
            new(Array.Empty<FoodCatalogItem>(), error);
    }
}
