using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    /// <summary>
    /// チュートリアルからの依頼を既存のゲーム機能に流し込む接着剤。
    ///
    /// チュートリアル側はここの存在を知らない(IGameCommandBus 越しにしか届かない)ので、
    /// 依存方向 Game ← Tutorial は発生しない。
    /// </summary>
    public class GameCommandRouter : MonoBehaviour
    {
        [Tooltip("S7 で前菜として皿に出す品目")]
        [SerializeField] private LocalFoods appetizer = LocalFoods.Curry;

        private IGameCommandBus _commandBus;
        private IFoodViewModel _foodViewModel;
        private IStandaloneWindowViewModel _standaloneWindowViewModel;

        [Inject]
        public void Construct(
            IGameCommandBus commandBus,
            IFoodViewModel foodViewModel,
            IStandaloneWindowViewModel standaloneWindowViewModel)
        {
            _commandBus = commandBus;
            _foodViewModel = foodViewModel;
            _standaloneWindowViewModel = standaloneWindowViewModel;
        }

        private void Start()
        {
            _commandBus.OnCommand.Subscribe(Handle).AddTo(this);
        }

        private void Handle(GameCommandId command)
        {
            switch (command)
            {
                case GameCommandId.ServeAppetizer:
                    _standaloneWindowViewModel.SpawnLocalFood(appetizer);
                    break;

                case GameCommandId.DestroyAllFood:
                    _foodViewModel.RequestDestroyFood();
                    break;

                case GameCommandId.ShowMenu:
                    _standaloneWindowViewModel.IsVisible.Value = true;
                    break;

                case GameCommandId.HideMenu:
                    _standaloneWindowViewModel.IsVisible.Value = false;
                    break;
            }
        }
    }
}
