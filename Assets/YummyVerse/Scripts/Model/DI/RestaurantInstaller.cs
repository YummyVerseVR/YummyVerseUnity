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
            // Container.BindInterfacesAndSelfTo<DummyFoodDownloader>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<FoodDownloader>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<QRDetectionService>().AsSingle().NonLazy();
        }
    }
}