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
            // 現在選択可能な MenuItem は Standalone catalog 由来だけ。
            // YummyService v2 の path/auth/download 契約が公開されるまで Network loader は生成しない。
            return new LocalFoodLoader(_localFoodSO);
        }
    }
}
