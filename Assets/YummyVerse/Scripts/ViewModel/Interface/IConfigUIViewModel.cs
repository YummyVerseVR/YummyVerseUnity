using System;
using System.Net;
using R3;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IConfigUIViewModel
    {
        ReactiveProperty<bool> IsVisible { get; } // 設定メニューを表示中かどうか
        ReactiveProperty<string> APIEndPointUrl { get; }
        ReactiveProperty<string> LastRequestHTTPStatus { get; }
        ReactiveProperty<string> LastRequestGuid { get; }
        ReactiveProperty<bool> IsStandaloneMode { get; }

        event Action OnAPIEndPointValidationError;
        ReactiveProperty<HttpStatusCode> ConnectionTestResult { get; }

        void UpdateEndPointUrl(string url);
        
        void SetStandaloneMode(bool isStandalone);
        
        void ConnectionTest();
    }
}