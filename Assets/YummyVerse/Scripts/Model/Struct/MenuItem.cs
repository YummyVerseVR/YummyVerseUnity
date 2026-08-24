using System;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Scripts.Model.Struct
{
    public enum MenuItemSource
    {
        BuiltIn = 0,
        PersistentData = 1,
        ApiV2 = 2
    }

    /// <summary>
    /// 来場者がメニューから選んだ食品。API v2 の opaque ID、端末保存食品、既存の
    /// built-in food を同じイベント境界へ載せる。QR identity とは独立している。
    /// </summary>
    public readonly struct MenuItem
    {
        public LocalFoods Food { get; }
        public Guid Guid { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string ModelLocation { get; }
        public MenuItemSource Source { get; }

        public bool IsValid => Source switch
        {
            MenuItemSource.BuiltIn => Guid != Guid.Empty,
            MenuItemSource.PersistentData or MenuItemSource.ApiV2 =>
                !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(ModelLocation),
            _ => false
        };

        public MenuItem(LocalFoods food, Guid guid)
        {
            Food = food;
            Guid = guid;
            Id = guid == Guid.Empty ? string.Empty : $"built-in:{guid:D}";
            DisplayName = food.ToString();
            ModelLocation = string.Empty;
            Source = MenuItemSource.BuiltIn;
        }

        public MenuItem(FoodCatalogItem item)
        {
            Food = default;
            Guid = Guid.Empty;
            Id = item?.Id ?? string.Empty;
            DisplayName = item?.DisplayName ?? string.Empty;
            ModelLocation = item?.ModelLocation ?? string.Empty;
            Source = item?.Source ?? MenuItemSource.PersistentData;
        }

        public override string ToString() => $"{DisplayName}({Id}, {Source})";
    }
}
