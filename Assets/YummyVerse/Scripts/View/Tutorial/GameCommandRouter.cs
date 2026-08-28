using R3;
using UnityEngine;
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
        private ILocalFoodSelectionProvider _localFoodSelectionProvider;
        private IFoodPlacementService _foodPlacementService;
        private IFoodEatingService _foodEatingService;

        [Inject]
        public void Construct(
            IGameCommandBus commandBus,
            IFoodViewModel foodViewModel,
            IStandaloneWindowViewModel standaloneWindowViewModel,
            IGameEventPublisher eventPublisher,
            ILocalFoodSelectionProvider localFoodSelectionProvider,
            IFoodPlacementService foodPlacementService,
            IFoodEatingService foodEatingService)
        {
            _commandBus = commandBus;
            _foodViewModel = foodViewModel;
            _standaloneWindowViewModel = standaloneWindowViewModel;
            _eventPublisher = eventPublisher;
            _localFoodSelectionProvider = localFoodSelectionProvider;
            _foodPlacementService = foodPlacementService;
            _foodEatingService = foodEatingService;
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

                case GameCommandId.ResetFoodState:
                    _foodViewModel.ResetFoodState();
                    break;

                case GameCommandId.ShowMenu:
                    _standaloneWindowViewModel.IsVisible.Value = true;
                    break;

                case GameCommandId.HideMenu:
                    _standaloneWindowViewModel.IsVisible.Value = false;
                    break;

                // 救済も実際のすくいと同じ経路を通す。チュートリアルは常に本物の
                // FoodScooped / DishCleared で進むため、時間経過だけで進むステップが無くなる。
                case GameCommandId.ForceScoopFood:
                    if (!_foodEatingService.TryScoop())
                    {
                        Debug.LogWarning("[Tutorial] すくえる食べ物がないため、すくいの救済を行えませんでした。");
                    }
                    break;

                case GameCommandId.ForceClearDish:
                    if (!_foodEatingService.ForceClear())
                    {
                        // 食べ物が無い/既に完食済みなら、少なくとも皿の上は空にしておく。
                        _foodViewModel.ResetFoodState();
                    }
                    break;
            }
        }

        private void ServeRandomPersistentFood()
        {
            if (!_localFoodSelectionProvider.TryGetSelected(out var selected))
            {
                Debug.LogWarning(
                    $"[Tutorial] 表示できるローカル食品がありません: " +
                    $"{Application.persistentDataPath}/Foods/*/model.glb");
                return;
            }

            Debug.Log($"[Tutorial] ランダムなローカル食品を表示します: {selected.DisplayName}");
            if (!_foodPlacementService.TryActivateDraftPoseForFood())
            {
                Debug.LogWarning("[Tutorial] 食品の配置座標が未設定です。設定画面で配置モデルを移動してください。");
            }
            _eventPublisher.PublishMenuItemSelected(new MenuItem(selected));
        }
    }
}
