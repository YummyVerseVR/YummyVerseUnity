using UnityEngine;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>Unity adapter for the persistent-data location used by the catalog source.</summary>
    public sealed class UnityPersistentFoodCatalogPath : IPersistentFoodCatalogPath
    {
        public string FoodsDirectory =>
            System.IO.Path.Combine(Application.persistentDataPath, "Foods");
    }
}
