using YummyVerse.Scripts.Model;
using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    public class QRInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<QRViewModel>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<QRValueValidator>().AsSingle().NonLazy();
        }
    }
}