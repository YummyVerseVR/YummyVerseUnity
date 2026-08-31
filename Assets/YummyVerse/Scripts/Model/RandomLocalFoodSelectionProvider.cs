using System;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// Foodsフォルダから1品をランダム選択し、配置プレビューとS5で共有する。
    /// </summary>
    public sealed class RandomLocalFoodSelectionProvider : ILocalFoodSelectionProvider
    {
        private readonly IPersistentFoodCatalogSource _persistentSource;
        private FoodCatalogItem _selected;

        public RandomLocalFoodSelectionProvider(IPersistentFoodCatalogSource persistentSource)
        {
            _persistentSource = persistentSource ?? throw new ArgumentNullException(nameof(persistentSource));
        }

        public bool TryGetSelected(out FoodCatalogItem item)
        {
            if (_selected != null && _selected.IsSelectable)
            {
                item = _selected;
                return true;
            }

            if (!_persistentSource.TrySelectRandom(out _selected))
            {
                item = null;
                return false;
            }

            item = _selected;
            return true;
        }
    }
}
