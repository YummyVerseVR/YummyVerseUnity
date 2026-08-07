using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    public class ConfigUIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<ConfigUIViewModel>().AsSingle();
            // StandaloneWindowViewModel はチュートリアル層からも参照するため
            // SharedViewModelInstaller でシーンスコープにバインドしている。
        }
    }
}
