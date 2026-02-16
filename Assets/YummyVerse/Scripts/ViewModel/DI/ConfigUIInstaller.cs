using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    public class ConfigUIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<ConfigUIViewModel>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<StandaloneWindowViewModel>().AsSingle().NonLazy();
        }
    }
}