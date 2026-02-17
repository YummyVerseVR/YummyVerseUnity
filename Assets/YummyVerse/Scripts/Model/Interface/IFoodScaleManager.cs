using R3;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodScaleManager
    {
        ReactiveProperty<float> FoodScale { get; }
        
        bool UpdateFoodScale(float scale);
    }
}