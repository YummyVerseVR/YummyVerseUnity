using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    /// <summary>
    /// 複数の機能から参照される ViewModel をシーンスコープでバインドする。
    /// SceneContext の MonoInstallers に登録すること。
    ///
    /// これらは元々 FoodView / YummyUI プレハブの GameObjectContext (サブコンテナ) に
    /// 置かれていたが、サブコンテナのバインドは親コンテナから解決できないため、
    /// チュートリアル層(シーンスコープ)から参照できるようここへ引き上げた。
    /// プレハブ内の View はサブコンテナが親を辿るので、そのまま同じインスタンスを受け取る。
    /// </summary>
    public class SharedViewModelInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<FoodViewModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<StandaloneWindowViewModel>().AsSingle();
        }
    }
}
