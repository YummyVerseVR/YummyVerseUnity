using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    public class FoodInstaller  : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<FoodViewModel>().AsSingle().NonLazy();
        }
    }
}