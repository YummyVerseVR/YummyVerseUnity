namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>Supplies the application-owned directory containing persistent food assets.</summary>
    public interface IPersistentFoodCatalogPath
    {
        string FoodsDirectory { get; }
    }
}
