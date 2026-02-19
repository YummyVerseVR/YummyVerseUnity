using System;
using System.Net;
using R3;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.ViewModel
{
    public class ConfigUIViewModel : IConfigUIViewModel, IInitializable, IDisposable
    {
        private readonly IEndPointManager _endPointManager;
        private readonly IFoodContext _foodContext;
        private readonly ISettingManager _settingManager;
        private readonly IFoodScaleManager _foodScaleManager;
        private readonly IInputLayer _inputLayer;
        private readonly IQRDetectionService _qrDetectionService;
        
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        
        public ReactiveProperty<bool> IsVisible { get; } = new(false);
        public ReactiveProperty<string> APIEndPointUrl { get; }  = new();
        public ReactiveProperty<string> LastRequestHTTPStatus { get; } = new();
        public ReactiveProperty<string> LastDetectedGuid { get; }  = new();
        public ReactiveProperty<bool> IsStandaloneMode { get; }  = new();
        
        public event Action OnAPIEndPointValidationError = delegate { };
        
        public ReactiveProperty<HttpStatusCode> ConnectionTestResult { get; } = new(0);

        public ConfigUIViewModel(IEndPointManager endPointManager, 
            IFoodContext foodContext, 
            ISettingManager settingManager,  
            IFoodScaleManager foodScaleManager,
            IInputLayer inputLayer,
            IQRDetectionService qrDetectionService
            )
        {
            _endPointManager = endPointManager;
            _foodContext = foodContext;
            _settingManager = settingManager;
            _foodScaleManager = foodScaleManager;
            _inputLayer = inputLayer;
            _qrDetectionService = qrDetectionService;
        }

        public void Initialize()
        {
            // ダウンロード結果が更新されたらStatusCodeを更新
            _foodContext.downloadResult.Subscribe(v =>
            {
                LastRequestHTTPStatus.Value = v.StatusCode.ToString();
            }).AddTo(_disposables);

            _qrDetectionService.OnChangeGUID.Subscribe(v =>
            {
                LastDetectedGuid.Value = v.ToString();
            }).AddTo(_disposables);
            
            // ボタンが押されたら表示状態を反転
            Observable.FromEvent(
                    h => _inputLayer.OnConfigUIButtonClicked += h,
                    h => _inputLayer.OnConfigUIButtonClicked -= h)
                .Subscribe(_ => IsVisible.Value = !IsVisible.Value).AddTo(_disposables);
        }
        
        public void Dispose()
        {
            _disposables?.Dispose();
            IsVisible?.Dispose();
            APIEndPointUrl?.Dispose();
            LastRequestHTTPStatus?.Dispose();
            LastDetectedGuid?.Dispose();
            IsStandaloneMode?.Dispose();
            ConnectionTestResult?.Dispose();
        }
        
        public void UpdateEndPointUrl(string url)
        { 
            var success = _endPointManager.UpdateEndPointUrl(url);
            if (success)
            {
                APIEndPointUrl.Value = url;
                return;
            }
            OnAPIEndPointValidationError.Invoke();
        }

        public void SetStandaloneMode(bool isStandalone)
        {
            IsStandaloneMode.Value = isStandalone;
            _settingManager.isStandaloneMode.Value = isStandalone;
        }

        public void SetFoodScale(float scale)
        {
            _foodScaleManager.UpdateFoodScale(scale);
        }

        public void ConnectionTest()
        {
            throw new NotImplementedException();
        }

    }
}