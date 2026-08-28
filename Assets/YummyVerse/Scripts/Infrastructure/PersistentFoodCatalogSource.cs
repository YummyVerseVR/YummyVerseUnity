using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
            return UniTask.FromResult(new FoodCatalogSourceResult(
                PersistentFoodCatalogScanner.Scan(_path.FoodsDirectory)));
        }

        public bool TrySelectRandom(out FoodCatalogItem item)
        {
            return PersistentFoodCatalogScanner.TrySelectRandom(
                _path.FoodsDirectory,
                _random,
                out item);
        }
    }
}
