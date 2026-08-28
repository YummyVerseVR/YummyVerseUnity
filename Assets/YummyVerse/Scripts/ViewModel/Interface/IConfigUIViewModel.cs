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
        ReactiveProperty<string> SpatialPlacementStatus { get; }
        ReactiveProperty<bool> IsSpatialAnchorReady { get; }
        ReactiveProperty<bool> IsFoodPositionFixed { get; }
        ReactiveProperty<bool> IsSpatialPlacementBusy { get; }

        event Action OnAPIEndPointValidationError;
        ReactiveProperty<TestConnectionResult> ConnectionTestResult { get; }

        void SetVisible(bool isVisible);

        /// <summary>
        /// 設定画面から現在のプレイを中断し、スタート待ちへ戻す。
        /// 実際の食品・セッション状態の初期化は実装側のセッション管理へ委譲する。
        /// </summary>
        void ResetToStart();

        void UpdateEndPointUrl(string url);
        
        void SetStandaloneMode(bool isStandalone);
        
        void SetFoodScale(float scale);

        UniTask SetSpatialAnchor(CancellationToken ct);

        UniTask FixFoodPosition(CancellationToken ct);
        
        UniTask ConnectionTest(CancellationToken ct);
    }
}
