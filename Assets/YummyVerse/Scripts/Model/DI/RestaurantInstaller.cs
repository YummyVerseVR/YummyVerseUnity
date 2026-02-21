using YummyVerse.Scripts.Model.Dummies;
using Zenject;

namespace YummyVerse.Scripts.Model.DI
{
    public class RestaurantInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EndPointManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<FoodContext>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<FoodFetchableFactory>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<QRDetectionService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SettingManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<FoodScaleManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<InputLayer>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<NetworkConnectionTester>().AsSingle().NonLazy();
        }
    }
}