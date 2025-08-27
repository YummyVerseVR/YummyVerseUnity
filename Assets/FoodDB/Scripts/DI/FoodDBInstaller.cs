using Food3DModel.Model;
using FoodDB.Scripts.ViewModel;
using UnityEngine;
using Zenject;

public class FoodDBInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<FoodRepository>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<FoodInstanceHandler>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<SoundViewModel>().AsSingle().NonLazy();
    }
}