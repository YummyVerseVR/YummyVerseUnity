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
            container.Bind<IYummyServiceV2ConfigStore>()
                .To<PersistentYummyServiceV2ConfigStore>()
                .AsSingle();
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

            // 被り直しで再センタリングが走るとワールド原点が部屋に対して動き、
            // 設定した食品位置も Spatial Anchor も現実からずれる。最初の着脱より前に
            // 押さえておく必要があるので NonLazy で常駐させる。
            container.BindInterfacesAndSelfTo<XrRecenterGuard>().AsSingle().NonLazy();

            // XR セッションの着脱監視と、それに追従する描画負荷の調整。
            // 誰も解決しなくても最初の着脱から動いている必要があるので NonLazy で常駐させる。
            // 観測と描画にしか効かない。体験の進行はここを見ない。
            container.BindInterfacesAndSelfTo<XrSessionMonitor>().AsSingle().NonLazy();
            container.BindInterfacesAndSelfTo<XrSuspensionRenderThrottle>().AsSingle().NonLazy();
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
