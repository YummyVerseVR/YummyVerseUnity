using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class FoodFetchableFactory : IFoodFetchableFactory
    {
        private readonly ISettingManager _settingManager;
        private readonly IEndPointManager _endPointManager;
        
        public FoodFetchableFactory(ISettingManager settingManager,  IEndPointManager endPointManager)
        {
            _settingManager = settingManager;
            _endPointManager = endPointManager;
        }
        
        public IFoodFetchable Create()
        {
            if (_settingManager.isStandaloneMode.Value) return new LocalFoodLoader();
            return new FoodDownloader(_endPointManager);
        }
    }
}