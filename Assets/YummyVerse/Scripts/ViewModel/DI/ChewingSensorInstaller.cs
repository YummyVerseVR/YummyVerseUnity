using UnityEngine;
using YummyVerse.Scripts.Infrastructure;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.View;
using YummyVerse.Scripts.ViewModel.Tutorial;
using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    /// <summary>
    /// 咀嚼計 (シリアル接続) 一式のバインド。SceneContext の MonoInstallers に登録すること。
    ///
    /// 通信そのものは ChewingSensorService が常駐で面倒を見るので、NonLazy で必ず起動させる。
    /// 咀嚼計が繋がっていなくても探索を空振りし続けるだけで、他の機能には影響しない。
    /// </summary>
    public class ChewingSensorInstaller : MonoInstaller
    {
        [SerializeField] private ChewingSensorConfig chewingSensorConfig;

        public override void InstallBindings()
        {
            if (chewingSensorConfig == null)
            {
                Debug.LogError(
                    "[ChewingSensorInstaller] chewingSensorConfig が未設定です。" +
                    "ChewingSensorConfig アセットを割り当ててください。");
                return;
            }

            Container.BindInstance(chewingSensorConfig).AsSingle();

            Container.Bind<ISerialPortProvider>().To<SerialPortProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<ChewingSensorService>().AsSingle().NonLazy();

            // スタート直後 (S2 の手前) に挟まる較正案内。SessionController から呼ばれる。
            Container.BindInterfacesAndSelfTo<ChewingCalibrationFlow>().AsSingle();

            // 咀嚼音は AudioSource を1つ持つだけなので、シーンに置かず実行時に用意する。
            Container.BindInterfacesAndSelfTo<ChewingSoundView>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
        }
    }
}
