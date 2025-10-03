using PCVR.Model;
using UnityEngine;
using Zenject;

public class QRCodeInstaller : Installer<QRCodeInstaller>
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<QRCodeManager>().AsSingle();
    }
}