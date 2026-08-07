using System;
using R3;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    /// <summary>
    /// 既存のローカル食品スポーン(StandaloneWindowViewModel)を「メニューが選ばれた」として
    /// イベントバスに流す橋渡し。GameEventBus 本体を ViewModel 層に依存させないためここに置く。
    /// </summary>
    public class MenuSelectionBridge : IInitializable, IDisposable
    {
        private readonly IStandaloneWindowViewModel _standaloneWindowViewModel;
        private readonly IGameEventPublisher _eventPublisher;
        private readonly LocalFoodSO _localFoodSO;

        private readonly CompositeDisposable _disposables = new();

        public MenuSelectionBridge(
            IStandaloneWindowViewModel standaloneWindowViewModel,
            IGameEventPublisher eventPublisher,
            LocalFoodSO localFoodSO)
        {
            _standaloneWindowViewModel = standaloneWindowViewModel;
            _eventPublisher = eventPublisher;
            _localFoodSO = localFoodSO;
        }

        public void Initialize()
        {
            Observable.FromEvent<LocalFoods>(
                    h => _standaloneWindowViewModel.OnLocalFoodSpawned += h,
                    h => _standaloneWindowViewModel.OnLocalFoodSpawned -= h)
                .Subscribe(food =>
                {
                    _localFoodSO.TryGetGuid(food, out var guid);
                    _eventPublisher.PublishMenuItemSelected(new MenuItem(food, guid));
                }).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
