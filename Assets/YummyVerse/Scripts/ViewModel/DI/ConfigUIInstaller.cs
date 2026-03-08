using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    public class ConfigUIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<ConfigUIViewModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<StandaloneWindowViewModel>().AsSingle();
        }
    }
}