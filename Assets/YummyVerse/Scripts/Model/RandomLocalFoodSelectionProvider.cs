using System.IO;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// Foodsフォルダから1品をランダム選択し、配置プレビューとS5で共有する。
    /// </summary>
    public sealed class RandomLocalFoodSelectionProvider : ILocalFoodSelectionProvider
    {
        private readonly System.Random _random = new();
        private FoodCatalogItem _selected;

        public bool TryGetSelected(out FoodCatalogItem item)
        {
            if (_selected != null && File.Exists(_selected.ModelLocation))
            {
                item = _selected;
                return true;
            }

            var foodsDirectory = Path.Combine(Application.persistentDataPath, "Foods");
            if (!PersistentFoodCatalogScanner.TrySelectRandom(foodsDirectory, _random, out _selected))
            {
                item = null;
                return false;
            }

            item = _selected;
            return true;
        }
    }
}
