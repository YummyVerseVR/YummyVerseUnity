using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
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
        private readonly IFoodPlacementService _foodPlacementService;
        private readonly INetworkConnectionTester _networkConnectionTester;
        
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        
        public ReactiveProperty<bool> IsVisible { get; } = new(false);
        public ReactiveProperty<string> APIEndPointUrl { get; }  = new();
        public ReactiveProperty<string> LastRequestHTTPStatus { get; } = new();
        public ReactiveProperty<string> LastDetectedGuid { get; }  = new();
        public ReactiveProperty<bool> IsStandaloneMode { get; }  = new();
        public ReactiveProperty<float> FoodScale { get; } = new();
        public ReactiveProperty<string> SpatialPlacementStatus { get; } = new();
        public ReactiveProperty<bool> IsSpatialAnchorReady { get; } = new();
        public ReactiveProperty<bool> IsFoodPositionFixed { get; } = new();
        public ReactiveProperty<bool> IsSpatialPlacementBusy { get; } = new();
        
        public event Action OnAPIEndPointValidationError = delegate { };
        
        // StatusCodeの初期値は未使用の値を設定
        public ReactiveProperty<TestConnectionResult> ConnectionTestResult { get; } = 
            new( new TestConnectionResult() {
                success =  false,
                StatusCode = (HttpStatusCode)(-1)
        });

        public ConfigUIViewModel(IEndPointManager endPointManager, 
            IFoodContext foodContext, 
            ISettingManager settingManager,  
            IFoodScaleManager foodScaleManager,
            IInputLayer inputLayer,
            IQRDetectionService qrDetectionService,
            IFoodPlacementService foodPlacementService,
            INetworkConnectionTester networkConnectionTester
            )
        {
            _endPointManager = endPointManager;
            _foodContext = foodContext;
            _settingManager = settingManager;
            _foodScaleManager = foodScaleManager;
            _inputLayer = inputLayer;
            _qrDetectionService = qrDetectionService;
            _foodPlacementService = foodPlacementService;
            _networkConnectionTester = networkConnectionTester;
        }

        public void Initialize()
        {
            // ダウンロード結果が更新されたらStatusCodeを更新
            // ConnectionErrorの場合は出力結果を上書き
            _foodContext.downloadResult.Subscribe(v =>
            {
                if (!v.success) LastRequestHTTPStatus.Value = "Network Connection Error";
                else LastRequestHTTPStatus.Value = v.StatusCode.ToString();
            }).AddTo(_disposables);

            _qrDetectionService.OnChangeGUID.Subscribe(v =>
            {
                LastDetectedGuid.Value = v.ToString();
            }).AddTo(_disposables);

            _foodPlacementService.StatusMessage
                .Subscribe(v => SpatialPlacementStatus.Value = v)
                .AddTo(_disposables);
            _foodPlacementService.IsAnchorReady
                .Subscribe(v => IsSpatialAnchorReady.Value = v)
                .AddTo(_disposables);
            _foodPlacementService.IsFoodPositionFixed
                .Subscribe(v => IsFoodPositionFixed.Value = v)
                .AddTo(_disposables);
            _foodPlacementService.IsBusy
                .Subscribe(v => IsSpatialPlacementBusy.Value = v)
                .AddTo(_disposables);
            
            // コントローラーのボタンが押されたら表示状態を反転
            Observable.FromEvent(
                    h => _inputLayer.OnConfigUIButtonClicked += h,
                    h => _inputLayer.OnConfigUIButtonClicked -= h)
                .Subscribe(_ =>
                {
                    IsVisible.Value = !IsVisible.Value;
                    _foodPlacementService.SetConfigurationVisible(IsVisible.Value);
                }).AddTo(_disposables);

            FoodScale.Value = _foodScaleManager.FoodScale.Value;
            APIEndPointUrl.Value = _endPointManager.baseEndPointUrl;
        }
        
        public void Dispose()
        {
            _foodPlacementService.SetConfigurationVisible(false);
            _disposables?.Dispose();
            IsVisible?.Dispose();
            APIEndPointUrl?.Dispose();
            LastRequestHTTPStatus?.Dispose();
            LastDetectedGuid?.Dispose();
            IsStandaloneMode?.Dispose();
            SpatialPlacementStatus?.Dispose();
            IsSpatialAnchorReady?.Dispose();
            IsFoodPositionFixed?.Dispose();
            IsSpatialPlacementBusy?.Dispose();
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
            var success = _foodScaleManager.UpdateFoodScale(scale);
            if(!success) return;
            FoodScale.Value = scale;
        }

        public async UniTask SetSpatialAnchor(CancellationToken ct)
        {
            await _foodPlacementService.SetAnchorAtDraftAsync(ct);
        }

        public async UniTask FixFoodPosition(CancellationToken ct)
        {
            await _foodPlacementService.FixFoodPositionAtDraftAsync(ct);
        }

        public async UniTask ConnectionTest(CancellationToken ct)
        {
            var result = await _networkConnectionTester.TestConnection(ct);
            ConnectionTestResult.OnNext(result); // 接続するたびに結果を強制通知
        }

    }
}
