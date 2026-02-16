using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "LocalFoodSOInstaller", menuName = "Installers/LocalFoodSOInstaller")]
public class LocalFoodSOInstaller : ScriptableObjectInstaller<LocalFoodSOInstaller>
{
    [SerializeField] private LocalFoodSO LocalFoodSo;
    public override void InstallBindings()
    {
        Container.BindInstance(LocalFoodSo).AsSingle().NonLazy();
    }
}