using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class FoodFetchableFactory : IFoodFetchableFactory
    {
        private readonly ISettingManager _settingManager;
        private readonly IEndPointManager _endPointManager;
        private readonly LocalFoodSO _localFoodSO;
        
        public FoodFetchableFactory(ISettingManager settingManager,  IEndPointManager endPointManager, LocalFoodSO localFoodSO)
        {
            _settingManager = settingManager;
            _endPointManager = endPointManager;
            _localFoodSO = localFoodSO;
        }
        
        public IFoodFetchable Create()
        {
            if (_settingManager.isStandaloneMode.Value) return new LocalFoodLoader(_localFoodSO);
            return new FoodDownloader(_endPointManager);
        }
    }
}