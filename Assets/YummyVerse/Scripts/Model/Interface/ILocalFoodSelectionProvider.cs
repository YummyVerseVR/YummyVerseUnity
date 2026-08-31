using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// 配置プレビューとチュートリアル本番で同じローカル食品を使うためのシーンスコープ選択。
    /// </summary>
    public interface ILocalFoodSelectionProvider
    {
        bool TryGetSelected(out FoodCatalogItem item);
    }
}
