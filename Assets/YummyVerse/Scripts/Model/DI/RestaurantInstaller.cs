using YummyVerse.Scripts.Model.Dummies;
using Zenject;

namespace YummyVerse.Scripts.Model.DI
{
    public class RestaurantInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EndPointManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<FoodContext>().AsSingle();
            Container.BindInterfacesAndSelfTo<FoodFetchableFactory>().AsSingle();
            Container.BindInterfacesAndSelfTo<QRDetectionService>().AsSingle();
            Container.BindInterfacesAndSelfTo<SettingManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<FoodScaleManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<InputLayer>().AsSingle();
            Container.BindInterfacesAndSelfTo<NetworkConnectionTester>().AsSingle();
        }
    }
}