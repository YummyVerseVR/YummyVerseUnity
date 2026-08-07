using System;
using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// ゲーム側が発火し、チュートリアルが購読するイベント契約(購読側)。
    /// チュートリアルの都合をゲーム側に漏らさないため、発火は IGameEventPublisher に分けてある。
    /// </summary>
    public interface IGameEventBus
    {
        event Action OnStartButtonPressed;
        event Action OnQrPlateDetected;      // 紙皿のQR認識成立
        event Action OnQrPlateLost;          // 認識ロスト
        event Action OnFoodScooped;          // 1回すくった
        event Action OnDishCleared;          // 完食
        event Action<MenuItem> OnMenuItemSelected;
        event Action OnUserAbsent;           // 一定時間の無操作／人検知ロスト

        /// <summary>
        /// 指定イベントのストリームを返す。TutorialCondition から id 指定で待機するために使う。
        /// 上の event Action 群と同じ発火源を共有しており、二重管理はしていない。
        /// </summary>
        Observable<Unit> GetStream(GameEventId id);

        /// <summary>
        /// 何らかのイベントが発火するたびに流れる。無操作タイマーのリセット等に使う。
        /// </summary>
        Observable<GameEventId> OnAnyEvent { get; }

        /// <summary>
        /// 直近に選択されたメニュー。未選択なら null。
        /// </summary>
        MenuItem? LastSelectedMenuItem { get; }
    }
}
