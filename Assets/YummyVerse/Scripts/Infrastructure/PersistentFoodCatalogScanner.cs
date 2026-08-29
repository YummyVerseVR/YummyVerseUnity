using System;
using System.Collections.Generic;
using System.IO;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// PersistentDataPath/Foods のファイル規約を、Unity API に依存せず走査する。
    /// </summary>
    public static class PersistentFoodCatalogScanner
    {
        private static readonly string[] PreviewFileNames =
        {
            "preview.png",
            "preview.jpg",
            "preview.jpeg",
            "preview.webp"
        };

        public static IReadOnlyList<FoodCatalogItem> Scan(string foodsDirectory)
        {
            var items = new List<FoodCatalogItem>();
            if (string.IsNullOrWhiteSpace(foodsDirectory) || !Directory.Exists(foodsDirectory))
            {
                return items;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(foodsDirectory);
                Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                return items;
            }
            catch (UnauthorizedAccessException)
            {
                return items;
            }

            foreach (var directory in directories)
            {
                var modelPath = Path.Combine(directory, "model.glb");
                if (!File.Exists(modelPath)) continue;

                var previewPath = FindPreview(directory);
                var foodName = new DirectoryInfo(directory).Name;
                if (string.IsNullOrWhiteSpace(foodName)) continue;

                items.Add(new FoodCatalogItem(
                    $"local:{foodName}",
                    foodName,
                    previewPath,
                    modelPath,
                    MenuItemSource.PersistentData));
            }

            return items;
        }

        public static bool TrySelectRandom(
            string foodsDirectory,
            Random random,
            out FoodCatalogItem selected)
        {
            selected = null;
            if (random == null) throw new ArgumentNullException(nameof(random));

            var items = Scan(foodsDirectory);
            if (items.Count == 0) return false;

            selected = items[random.Next(items.Count)];
            return true;
        }

        private static string FindPreview(string directory)
        {
            foreach (var fileName in PreviewFileNames)
            {
                var path = Path.Combine(directory, fileName);
                if (File.Exists(path)) return path;
            }

            return string.Empty;
        }
    }
}
