using System;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// Transport-only representation of the v2 menu response.
    /// It deliberately remains separate from <see cref="FoodCatalogItem"/>.
    ///
    /// This type remains only for an explicitly requested public sample-menu
    /// consumer.  The generated-food runtime adapter uses
    /// <see cref="DeviceOrderListResponseDto"/> and /v2/devices/unity/orders.
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

        /// <summary>
        /// 咀嚼音サンプルのURL。v2 の PublicMenuItem で規範化されている音声フィールドはこれだけで、
        /// sample_audio_url / audio_url は contract に存在しない。
        /// sample_glb_url と同じく string | null なので、欠けていれば咀嚼音なしとして扱う。
        /// </summary>
        public string sample_wav_url;
    }
}
