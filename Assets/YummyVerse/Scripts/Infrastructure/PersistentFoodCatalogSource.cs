using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// PersistentDataPath adapter. File layout knowledge is kept in
    /// <see cref="PersistentFoodCatalogScanner"/> so the application source remains
    /// a small boundary adapter.
    /// </summary>
    public sealed class PersistentFoodCatalogSource : IPersistentFoodCatalogSource
    {
        private readonly IPersistentFoodCatalogPath _path;
        private readonly Random _random = new();

        public PersistentFoodCatalogSource(IPersistentFoodCatalogPath path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public UniTask<FoodCatalogSourceResult> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureBundledFoods();
            return UniTask.FromResult(new FoodCatalogSourceResult(
                PersistentFoodCatalogScanner.Scan(_path.FoodsDirectory)));
        }

        public bool TrySelectRandom(out FoodCatalogItem item)
        {
            EnsureBundledFoods();
            return PersistentFoodCatalogScanner.TrySelectRandom(
                _path.FoodsDirectory,
                _random,
                out item);
        }

        private void EnsureBundledFoods()
        {
            try
            {
                BundledFoodSeeder.CopyMissingFiles(
                    Application.streamingAssetsPath,
                    _path.FoodsDirectory);
            }
            catch (Exception exception)
            {
                // Bundled samples improve the first-run experience, but a copy failure
                // must not make foods the user already installed unavailable.
                Debug.LogWarning($"Could not copy bundled local food data: {exception.GetType().Name}");
            }
        }
    }
}
