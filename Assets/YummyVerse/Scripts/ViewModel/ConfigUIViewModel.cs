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
        
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        
        public ReactiveProperty<bool> IsVisible { get; } = new();
        public ReactiveProperty<string> APIEndPointUrl { get; }  = new();
        public ReactiveProperty<string> LastRequestHTTPStatus { get; } = new();
        public ReactiveProperty<string> LastRequestGuid { get; }  = new();
        public ReactiveProperty<bool> IsStandaloneMode { get; }  = new();
        
        public event Action OnAPIEndPointValidationError = delegate { };
        
        public ReactiveProperty<HttpStatusCode> ConnectionTestResult { get; } = new(0);

        public void Initialize()
        {
            // ダウンロード結果が更新されたらViewModel側でも更新
            _foodContext.downloadResult.Subscribe(v =>
            {
                LastRequestHTTPStatus.Value = v.StatusCode.ToString();
                LastRequestGuid.Value = v.RequestedGuid.ToString();
            }).AddTo(_disposables);
        }
        
        public void Dispose()
        {
            _disposables?.Dispose();
            IsVisible?.Dispose();
            APIEndPointUrl?.Dispose();
            LastRequestHTTPStatus?.Dispose();
            LastRequestGuid?.Dispose();
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

        public void ConnectionTest()
        {
            throw new NotImplementedException();
        }

    }
}