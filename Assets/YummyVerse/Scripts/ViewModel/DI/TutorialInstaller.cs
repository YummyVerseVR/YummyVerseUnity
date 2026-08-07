using UnityEngine;
using YummyVerse.Scripts.View.Tutorial;
using YummyVerse.Scripts.ViewModel.Tutorial;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;
using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    /// <summary>
    /// チュートリアル一式のバインド。SceneContext の MonoInstallers に登録すること。
    ///
    /// Presenter の View は SceneContext 配下に置く必要がある
    /// (GameObjectContext のサブコンテナに入れると親コンテナから解決できない)。
    /// </summary>
    public class TutorialInstaller : MonoInstaller
    {
        [SerializeField] private TutorialConfig tutorialConfig;

        public override void InstallBindings()
        {
            if (tutorialConfig == null)
            {
                Debug.LogError("[TutorialInstaller] tutorialConfig が未設定です。TutorialConfig アセットを割り当ててください。");
                return;
            }

            Container.BindInstance(tutorialConfig).AsSingle();

            // Presenter (状態は ReactiveProperty、View は購読するだけ)
            Container.BindInterfacesAndSelfTo<MessagePresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<HintPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<FeedbackPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<VoicePresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<ChoicePresenter>().AsSingle();

            // 進行
            Container.BindInterfacesAndSelfTo<TutorialContext>().AsSingle();
            Container.BindInterfacesAndSelfTo<TutorialRunner>().AsSingle();
            Container.BindInterfacesAndSelfTo<FreePlayFlow>().AsSingle();

            // 既存機能との接着
            Container.BindInterfacesAndSelfTo<MenuSelectionBridge>().AsSingle().NonLazy();

            // セッション。常駐ループを持つので NonLazy で必ず起動させる。
            Container.BindInterfacesAndSelfTo<SessionController>().AsSingle().NonLazy();
        }
    }
}
