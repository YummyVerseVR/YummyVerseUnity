using System;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// Transport-only representation of the v2 menu response.
    /// It deliberately remains separate from <see cref="FoodCatalogItem"/>.
    /// </summary>
    [Serializable]
    public sealed class MenuResponseDto
    {
        public MenuItemDto[] items;
    }

    [Serializable]
    public sealed class MenuItemDto
    {
        public string id;
        public string display_name;
        public bool available;
        public string thumbnail_url;
        public string sample_glb_url;
    }
}
