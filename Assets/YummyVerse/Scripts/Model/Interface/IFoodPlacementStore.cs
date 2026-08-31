using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodPlacementStore
    {
        bool TryLoad(out FoodPlacementData data);
        void Save(FoodPlacementData data);
    }
}
