using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// Copies repository-bundled foods to PersistentDataPath/Foods without
    /// replacing files the user has already installed or customized.
    /// </summary>
    internal static class BundledFoodSeeder
    {
        private const string BundledFoodsDirectoryName = "Foods";
        private const string ManifestFileName = "default-foods.json";

        private static readonly HashSet<string> AllowedFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "model.glb",
            "preview.png",
            "preview.jpg",
            "preview.jpeg",
            "audio.wav",
            "audio.mp3",
            "audio.ogg"
        };

        [Serializable]
        private sealed class Manifest
        {
            public FoodEntry[] foods;
        }

        [Serializable]
        private sealed class FoodEntry
        {
            public string directoryName;
            public string[] files;
        }

        public static void CopyMissingFiles(string streamingAssetsPath, string foodsDirectory)
        {
            if (string.IsNullOrWhiteSpace(streamingAssetsPath) || string.IsNullOrWhiteSpace(foodsDirectory))
            {
                return;
            }

            var bundledFoodsDirectory = Path.Combine(streamingAssetsPath, BundledFoodsDirectoryName);
            var manifestPath = Path.Combine(bundledFoodsDirectory, ManifestFileName);
            if (!File.Exists(manifestPath)) return;

            Manifest manifest;
            try
            {
                manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read bundled food manifest: {exception.GetType().Name}");
                return;
            }

            if (manifest?.foods == null) return;

            foreach (var food in manifest.foods)
            {
                if (food == null || !IsSafeDirectoryName(food.directoryName) || food.files == null) continue;

                var sourceFoodDirectory = Path.Combine(bundledFoodsDirectory, food.directoryName);
                var destinationFoodDirectory = Path.Combine(foodsDirectory, food.directoryName);
                foreach (var fileName in food.files)
                {
                    if (!AllowedFileNames.Contains(fileName)) continue;

                    var sourcePath = Path.Combine(sourceFoodDirectory, fileName);
                    var destinationPath = Path.Combine(destinationFoodDirectory, fileName);
                    if (!File.Exists(sourcePath) || File.Exists(destinationPath)) continue;

                    try
                    {
                        Directory.CreateDirectory(destinationFoodDirectory);
                        CopyWithoutOverwrite(sourcePath, destinationPath);
                    }
                    catch (IOException exception)
                    {
                        Debug.LogWarning($"Could not seed bundled food file '{food.directoryName}/{fileName}': {exception.GetType().Name}");
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        Debug.LogWarning($"Could not seed bundled food file '{food.directoryName}/{fileName}': {exception.GetType().Name}");
                    }
                }
            }
        }

        private static bool IsSafeDirectoryName(string directoryName)
        {
            return !string.IsNullOrWhiteSpace(directoryName)
                   && directoryName != "."
                   && directoryName != ".."
                   && directoryName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                   && !directoryName.Contains("/")
                   && !directoryName.Contains("\\");
        }

        private static void CopyWithoutOverwrite(string sourcePath, string destinationPath)
        {
            if (File.Exists(destinationPath)) return;

            var temporaryPath = destinationPath + ".seed-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(sourcePath, temporaryPath, false);
                if (File.Exists(destinationPath)) return;

                try
                {
                    File.Move(temporaryPath, destinationPath);
                }
                catch (IOException) when (File.Exists(destinationPath))
                {
                    // Another startup or user copy created the destination first.
                }
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }
}
