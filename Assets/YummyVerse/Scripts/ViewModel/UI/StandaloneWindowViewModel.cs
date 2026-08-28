using System;
using R3;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.ViewModel
{
    public class StandaloneWindowViewModel : IStandaloneWindowViewModel, IInitializable, IDisposable
    {
        private readonly ISettingManager _settingManager;
        private readonly LocalFoodSO _localFoodSO;
        
        private readonly CompositeDisposable _disposables = new();
        
        public StandaloneWindowViewModel(ISettingManager settingManager, LocalFoodSO localFoodSO)
        {
            _settingManager = settingManager;
            _localFoodSO = localFoodSO;
        }

        public ReactiveProperty<bool> IsVisible { get; } = new();

        public event Action<LocalFoods> OnLocalFoodSpawned;

        public void Initialize()
        {
            _settingManager.isStandaloneMode.Subscribe(SetVisible).AddTo(_disposables);
        }
        
        public void SpawnLocalFood(LocalFoods food)
        {
            if (!_localFoodSO.TryGetGuid(food, out _)) return;

            // 食品 selection は MenuSelectionBridge から game event として発行する。
            // QR detection service へ食品 GUID を流すと designation と identity が再結合するため使用しない。
            OnLocalFoodSpawned?.Invoke(food);
        }

        public void SetVisible(bool isVisible)
        {
            IsVisible.Value = isVisible;
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            IsVisible?.Dispose();
        }
    }
}
