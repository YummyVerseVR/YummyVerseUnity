using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IConfigUIViewModel
    {
        ReactiveProperty<bool> IsVisible { get; } // 設定メニューを表示中かどうか
        ReactiveProperty<string> APIEndPointUrl { get; }
        ReactiveProperty<string> LastRequestHTTPStatus { get; }
        ReactiveProperty<string> LastDetectedGuid { get; }
        ReactiveProperty<bool> IsStandaloneMode { get; }
        ReactiveProperty<float> FoodScale { get; }

        event Action OnAPIEndPointValidationError;
        ReactiveProperty<TestConnectionResult> ConnectionTestResult { get; }

        void UpdateEndPointUrl(string url);
        
        void SetStandaloneMode(bool isStandalone);
        
        void SetFoodScale(float scale);
        
        UniTask ConnectionTest(CancellationToken ct);
    }
}