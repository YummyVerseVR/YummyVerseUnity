using R3;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class FoodScaleManager : IFoodScaleManager
    {
        public ReactiveProperty<float> FoodScale { get; } = new ReactiveProperty<float>(0.5f);

        public bool UpdateFoodScale(float scale)
        {
            if(scale <= 0) return false;
            FoodScale.Value = scale;
            return true;
        }
    }
}