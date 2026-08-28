using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// Application service that composes catalog sources. Transport and filesystem
    /// concerns are implemented by source adapters, not by this orchestration class.
    /// Remote items retain their existing order and persistent items are appended, which
    /// preserves the menu behavior while making each source independently testable.
    /// </summary>
    public sealed class FoodCatalogService : IFoodCatalogService
    {
        private readonly IRemoteFoodCatalogSource _remoteSource;
        private readonly IPersistentFoodCatalogSource _persistentSource;

        public FoodCatalogService(
            IRemoteFoodCatalogSource remoteSource,
            IPersistentFoodCatalogSource persistentSource)
        {
            _remoteSource = remoteSource ?? throw new ArgumentNullException(nameof(remoteSource));
            _persistentSource = persistentSource ?? throw new ArgumentNullException(nameof(persistentSource));
        }

        public async UniTask<FoodCatalogLoadResult> LoadAsync(CancellationToken cancellationToken)
        {
            var remote = await _remoteSource.LoadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var persistent = await _persistentSource.LoadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return Combine(remote, persistent);
        }

        internal static FoodCatalogLoadResult Combine(
            FoodCatalogSourceResult remote,
            FoodCatalogSourceResult persistent)
        {
            var items = new List<FoodCatalogItem>();
            var errors = new List<string>();
            Append(remote, items, errors);
            Append(persistent, items, errors);

            return new FoodCatalogLoadResult(items, string.Join(" ", errors));
        }

        private static void Append(
            FoodCatalogSourceResult source,
            ICollection<FoodCatalogItem> items,
            ICollection<string> errors)
        {
            if (source == null) return;
            if (source.Items != null)
            {
                foreach (var item in source.Items) items.Add(item);
            }

            if (source.HasError) errors.Add(source.Error);
        }
    }
}
