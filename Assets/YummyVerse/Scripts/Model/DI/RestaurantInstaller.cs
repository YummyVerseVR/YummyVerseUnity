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
            Container.BindInterfacesAndSelfTo<PlayerPrefsFoodPlacementStore>().AsSingle();
            Container.BindInterfacesAndSelfTo<MetaSpatialAnchorBackend>().AsSingle();
            Container.BindInterfacesAndSelfTo<FoodPlacementService>().AsSingle();
            Container.BindInterfacesAndSelfTo<InputLayer>().AsSingle();
            Container.BindInterfacesAndSelfTo<NetworkConnectionTester>().AsSingle();
            Container.BindInterfacesAndSelfTo<FoodCatalogService>().AsSingle();
            Container.BindInterfacesAndSelfTo<RandomLocalFoodSelectionProvider>().AsSingle();

            // チュートリアル基盤 (ゲーム機能側と共有するため Model 層に置く)
            Container.BindInterfacesAndSelfTo<GameEventBus>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameCommandBus>().AsSingle();
            Container.BindInterfacesAndSelfTo<AppStateMachine>().AsSingle();
            Container.BindInterfacesAndSelfTo<IdleWatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameResetter>().AsSingle();
            Container.BindInterfacesAndSelfTo<TutorialAnalytics>().AsSingle();
        }
    }
}
