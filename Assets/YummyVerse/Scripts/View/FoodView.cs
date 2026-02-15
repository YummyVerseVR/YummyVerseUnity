using Zenject;

namespace YummyVerse.Scripts.View
{
    public class FoodView
    {
        private IFoodViewModel _foodViewModel;

        [Inject]
        public void Construct(IFoodViewModel foodViewModel)
        {
            _foodViewModel = foodViewModel;
        }
        
    }
}