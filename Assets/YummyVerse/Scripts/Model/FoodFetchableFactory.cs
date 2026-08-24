using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class FoodFetchableFactory : IFoodFetchableFactory
    {
        private readonly LocalFoodSO _localFoodSO;
        
        public FoodFetchableFactory(LocalFoodSO localFoodSO)
        {
            _localFoodSO = localFoodSO;
        }
        
        public IFoodFetchable Create()
        {
            return new FoodLoaderRouter(
                new LocalFoodLoader(_localFoodSO),
                new NetworkFoodLoader());
        }
    }
}
