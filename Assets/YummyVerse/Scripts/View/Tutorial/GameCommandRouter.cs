using System.IO;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
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
        private IGameCommandBus _commandBus;
        private IFoodViewModel _foodViewModel;
        private IStandaloneWindowViewModel _standaloneWindowViewModel;
        private IGameEventPublisher _eventPublisher;
        private readonly System.Random _random = new();

        [Inject]
        public void Construct(
            IGameCommandBus commandBus,
            IFoodViewModel foodViewModel,
            IStandaloneWindowViewModel standaloneWindowViewModel,
            IGameEventPublisher eventPublisher)
        {
            _commandBus = commandBus;
            _foodViewModel = foodViewModel;
            _standaloneWindowViewModel = standaloneWindowViewModel;
            _eventPublisher = eventPublisher;
        }

        private void Start()
        {
            _commandBus.OnCommand.Subscribe(Handle).AddTo(this);
        }

        private void Handle(GameCommandId command)
        {
            switch (command)
            {
                case GameCommandId.ServeRandomPersistentFood:
                    ServeRandomPersistentFood();
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

        private void ServeRandomPersistentFood()
        {
            var foodsDirectory = Path.Combine(Application.persistentDataPath, "Foods");
            if (!PersistentFoodCatalogScanner.TrySelectRandom(foodsDirectory, _random, out var selected))
            {
                Debug.LogWarning(
                    $"[Tutorial] 表示できるローカル食品がありません: {foodsDirectory}/*/model.glb");
                return;
            }

            Debug.Log($"[Tutorial] ランダムなローカル食品を表示します: {selected.DisplayName}");
            _eventPublisher.PublishMenuItemSelected(new MenuItem(selected));
        }
    }
}
