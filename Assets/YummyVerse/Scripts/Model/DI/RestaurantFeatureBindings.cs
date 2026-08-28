using YummyVerse.Scripts.Infrastructure;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Presentation;
using YummyVerse.Scripts.ViewModel.Tutorial;
using Zenject;

namespace YummyVerse.Scripts.Model.DI
{
    /// <summary>
    /// Feature-level registrations keep the SceneContext installer as a composition
    /// root. Each module owns one dependency slice and exposes only its ports.
    /// </summary>
    internal static class RestaurantCoreBindings
    {
        public static void Install(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<EndPointManager>().AsSingle();
            container.BindInterfacesAndSelfTo<FoodContext>().AsSingle();
            container.BindInterfacesAndSelfTo<QRDetectionService>().AsSingle();
            container.BindInterfacesAndSelfTo<SettingManager>().AsSingle();
            container.BindInterfacesAndSelfTo<FoodScaleManager>().AsSingle();
            container.BindInterfacesAndSelfTo<PlayerPrefsFoodPlacementStore>().AsSingle();
            container.BindInterfacesAndSelfTo<MetaSpatialAnchorBackend>().AsSingle();
            container.BindInterfacesAndSelfTo<FoodPlacementService>().AsSingle();
            container.BindInterfacesAndSelfTo<InputLayer>().AsSingle();
            container.BindInterfacesAndSelfTo<NetworkConnectionTester>().AsSingle();
            container.BindInterfacesAndSelfTo<GameEventBus>().AsSingle();
            container.BindInterfacesAndSelfTo<GameCommandBus>().AsSingle();
            container.BindInterfacesAndSelfTo<AppStateMachine>().AsSingle();
            container.BindInterfacesAndSelfTo<IdleWatcher>().AsSingle();
            container.BindInterfacesAndSelfTo<GameResetter>().AsSingle();
            container.BindInterfacesAndSelfTo<TutorialAnalytics>().AsSingle();
        }
    }

    internal static class RestaurantCatalogBindings
    {
        public static void Install(DiContainer container)
        {
            // Catalog metadata sources are composed once and shared by the menu and
            // local-food selection policy. No selection can create another loader graph.
            container.Bind<IPersistentFoodCatalogPath>()
                .To<UnityPersistentFoodCatalogPath>()
                .AsSingle();
            container.Bind<IRemoteFoodCatalogSource>()
                .To<NetworkFoodCatalogSource>()
                .AsSingle();
            container.Bind<IPersistentFoodCatalogSource>()
                .To<PersistentFoodCatalogSource>()
                .AsSingle();
            container.BindInterfacesAndSelfTo<FoodCatalogService>().AsSingle();
            container.BindInterfacesAndSelfTo<RandomLocalFoodSelectionProvider>().AsSingle();

            container.BindInterfacesAndSelfTo<LocalFoodLoader>().AsSingle();
            container.BindInterfacesAndSelfTo<NetworkFoodLoader>().AsSingle();
            container.BindInterfacesAndSelfTo<FoodLoaderRouter>().AsSingle();
        }
    }

    internal static class RestaurantInteractionBindings
    {
        public static void Install(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<FoodEatingService>().AsSingle();
            container.BindInterfacesAndSelfTo<OvrScoopProbeProvider>().AsSingle();
            container.BindInterfacesAndSelfTo<OvrScoopHaptics>().AsSingle();
            container.Bind<FoodRuntimePresenter>().AsTransient();
            container.Bind<FoodPlacementPreviewController>().AsTransient();
        }
    }

    internal static class RestaurantCommandBindings
    {
        public static void Install(DiContainer container)
        {
            // This is an application handler, not a scene View. NonLazy ensures the
            // command bus has one lifetime-bound consumer before tutorial steps run.
            container.BindInterfacesAndSelfTo<GameCommandHandler>()
                .AsSingle()
                .NonLazy();
        }
    }
}
