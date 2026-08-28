using Zenject;
using YummyVerse.Scripts.Presentation;

namespace YummyVerse.Scripts.ViewModel.DI
{
    public class ConfigUIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<ConfigUIViewModel>().AsSingle();
            Container.Bind<ConfigUIPresenter>().AsTransient();
            Container.Bind<VirtualKeyboardInputSourceBinder>().AsTransient();
            Container.Bind<VirtualKeyboardPlacement>().AsTransient();
            // StandaloneWindowViewModel はチュートリアル層からも参照するため
            // SharedViewModelInstaller でシーンスコープにバインドしている。
        }
    }
}
