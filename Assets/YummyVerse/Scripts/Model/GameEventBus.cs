using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// ゲーム機能 → チュートリアルの一方向のイベント経路。
    ///
    /// 既存実装から橋渡しできるものはここで配線している:
    ///   StartButtonPressed ← IInputLayer.OnStartButtonPressed
    ///   QrPlateDetected    ← IQRDetectionService.OnChangeTransform (毎回の検出で発火する)
    ///   QrPlateLost        ← IQRDetectionService.OnLost
    /// 発火元の実装がまだ無いもの(FoodScooped / DishCleared / UserAbsent)は
    /// IGameEventPublisher 経由で外から叩く。
    /// </summary>
    public class GameEventBus : IGameEventBus, IGameEventPublisher, IInitializable, IDisposable
    {
        private readonly IInputLayer _inputLayer;
        private readonly IQRDetectionService _qrDetectionService;

        private readonly CompositeDisposable _disposables = new();

        private readonly Dictionary<GameEventId, Subject<Unit>> _streams = new();
        private readonly Subject<GameEventId> _onAnyEvent = new();

        public event Action OnStartButtonPressed;
        public event Action OnQrPlateDetected;
        public event Action OnQrPlateLost;
        public event Action OnFoodScooped;
        public event Action OnDishCleared;
        public event Action<MenuItem> OnMenuItemSelected;
        public event Action OnUserAbsent;

        public Observable<GameEventId> OnAnyEvent => _onAnyEvent;
        public MenuItem? LastSelectedMenuItem { get; private set; }

        public GameEventBus(IInputLayer inputLayer, IQRDetectionService qrDetectionService)
        {
            _inputLayer = inputLayer;
            _qrDetectionService = qrDetectionService;

            foreach (GameEventId id in Enum.GetValues(typeof(GameEventId)))
            {
                _streams[id] = new Subject<Unit>();
            }
        }

        public Observable<Unit> GetStream(GameEventId id) => _streams[id];

        public void Initialize()
        {
            // 決定/スタートボタン
            Observable.FromEvent(
                    h => _inputLayer.OnStartButtonPressed += h,
                    h => _inputLayer.OnStartButtonPressed -= h)
                .Subscribe(_ =>
                {
                    OnStartButtonPressed?.Invoke();
                    Raise(GameEventId.StartButtonPressed);
                }).AddTo(_disposables);

            // QR認識成立。NotifyDetectQR は同一 Transform でも強制的に OnNext するため、
            // 再認識のたびにここへ流れてくる。初期値と Reset() の null は弾く。
            _qrDetectionService.OnChangeTransform
                .Where(t => t != null)
                .Subscribe(_ =>
                {
                    OnQrPlateDetected?.Invoke();
                    Raise(GameEventId.QrPlateDetected);
                }).AddTo(_disposables);

            // QR認識ロスト
            _qrDetectionService.OnLost
                .Subscribe(_ =>
                {
                    OnQrPlateLost?.Invoke();
                    Raise(GameEventId.QrPlateLost);
                }).AddTo(_disposables);
        }

        public void PublishFoodScooped()
        {
            OnFoodScooped?.Invoke();
            Raise(GameEventId.FoodScooped);
        }

        public void PublishDishCleared()
        {
            OnDishCleared?.Invoke();
            Raise(GameEventId.DishCleared);
        }

        public void PublishMenuItemSelected(MenuItem item)
        {
            LastSelectedMenuItem = item;
            OnMenuItemSelected?.Invoke(item);
            Raise(GameEventId.MenuItemSelected);
        }

        public void PublishUserAbsent()
        {
            OnUserAbsent?.Invoke();
            Raise(GameEventId.UserAbsent);
        }

        /// <summary>
        /// セッション終了時に持ち越してはいけない状態を捨てる。
        /// </summary>
        public void ResetSessionState()
        {
            LastSelectedMenuItem = null;
        }

        private void Raise(GameEventId id)
        {
            Debug.Log($"[GameEvent] {id}");
            _streams[id].OnNext(Unit.Default);
            _onAnyEvent.OnNext(id);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            foreach (var stream in _streams.Values) stream.Dispose();
            _streams.Clear();
            _onAnyEvent?.Dispose();
        }
    }
}
