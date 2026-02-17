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
        private readonly IQRDetectionService _qrDetectionService;
        private readonly LocalFoodSO _localFoodSO;
        
        private readonly CompositeDisposable _disposables = new();
        
        public StandaloneWindowViewModel(ISettingManager settingManager,  IQRDetectionService qrDetectionService, LocalFoodSO localFoodSO)
        {
            _settingManager = settingManager;
            _qrDetectionService = qrDetectionService;
            _localFoodSO = localFoodSO;
        }

        public ReactiveProperty<bool> IsVisible { get; } = new();
        
        public void Initialize()
        {
            _settingManager.isStandaloneMode.Subscribe(v => IsVisible.Value = v).AddTo(_disposables);
        }
        
        public void SpawnLocalFood(LocalFoods food)
        {
            if(!_localFoodSO.TryGetGuid(food, out var localFoodGuid)) return;
            _qrDetectionService.NotifyDetectQR(localFoodGuid, _qrDetectionService.OnChangeTransform.Value);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            IsVisible?.Dispose();
        }
    }
}