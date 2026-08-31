using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    /// <summary>
    /// Application-side command handler for tutorial requests. It owns the mapping from
    /// command identifiers to use-case ports; no Unity component or scene object is
    /// required, and the handler is started once by the SceneContext composition root.
    /// </summary>
    public sealed class GameCommandHandler : IInitializable, IDisposable
    {
        private readonly IGameCommandBus _commandBus;
        private readonly IFoodViewModel _foodViewModel;
        private readonly IStandaloneWindowViewModel _standaloneWindowViewModel;
        private readonly IGameEventPublisher _eventPublisher;
        private readonly ILocalFoodSelectionProvider _localFoodSelectionProvider;
        private readonly IFoodPlacementService _foodPlacementService;
        private readonly IFoodEatingService _foodEatingService;
        private readonly CompositeDisposable _disposables = new();

        public GameCommandHandler(
            IGameCommandBus commandBus,
            IFoodViewModel foodViewModel,
            IStandaloneWindowViewModel standaloneWindowViewModel,
            IGameEventPublisher eventPublisher,
            ILocalFoodSelectionProvider localFoodSelectionProvider,
            IFoodPlacementService foodPlacementService,
            IFoodEatingService foodEatingService)
        {
            _commandBus = commandBus ?? throw new ArgumentNullException(nameof(commandBus));
            _foodViewModel = foodViewModel ?? throw new ArgumentNullException(nameof(foodViewModel));
            _standaloneWindowViewModel = standaloneWindowViewModel
                                         ?? throw new ArgumentNullException(nameof(standaloneWindowViewModel));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _localFoodSelectionProvider = localFoodSelectionProvider
                                          ?? throw new ArgumentNullException(nameof(localFoodSelectionProvider));
            _foodPlacementService = foodPlacementService
                                    ?? throw new ArgumentNullException(nameof(foodPlacementService));
            _foodEatingService = foodEatingService
                                 ?? throw new ArgumentNullException(nameof(foodEatingService));
        }

        public void Initialize()
        {
            _commandBus.OnCommand.Subscribe(Handle).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
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
                    _standaloneWindowViewModel.SetVisible(true);
                    break;
                case GameCommandId.HideMenu:
                    _standaloneWindowViewModel.SetVisible(false);
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
                Debug.LogWarning("[Tutorial] 表示できるローカル食品がありません。");
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
