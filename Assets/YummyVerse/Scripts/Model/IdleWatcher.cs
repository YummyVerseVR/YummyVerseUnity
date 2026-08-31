using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// バス上の任意のイベントを「操作あり」とみなし、無操作が続いたら UserAbsent を発火する。
    /// 人検知の実装が入るまでの合成的な代替。
    /// </summary>
    public class IdleWatcher : IIdleWatcher, IInitializable, ITickable, IDisposable
    {
        private readonly IGameEventBus _gameEventBus;
        private readonly IGameEventPublisher _gameEventPublisher;
        private readonly CompositeDisposable _disposables = new();

        private bool _active;
        private float _elapsed;

        public float IdleTimeoutSeconds { get; set; } = 90f;

        public IdleWatcher(IGameEventBus gameEventBus, IGameEventPublisher gameEventPublisher)
        {
            _gameEventBus = gameEventBus;
            _gameEventPublisher = gameEventPublisher;
        }

        public void Initialize()
        {
            // UserAbsent 自身で巻き戻すと発火し続けるので除外する
            _gameEventBus.OnAnyEvent
                .Where(id => id != Struct.GameEventId.UserAbsent)
                .Subscribe(_ => NotifyActivity())
                .AddTo(_disposables);
        }

        public void SetActive(bool active)
        {
            _active = active;
            _elapsed = 0f;
        }

        public void NotifyActivity()
        {
            _elapsed = 0f;
        }

        public void Tick()
        {
            if (!_active) return;
            if (IdleTimeoutSeconds <= 0f) return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < IdleTimeoutSeconds) return;

            // 発火は1回だけ。停止して以降のセッションで SetActive(true) されるのを待つ。
            _active = false;
            _elapsed = 0f;
            Debug.Log($"[IdleWatcher] {IdleTimeoutSeconds}秒間の無操作を検知したため UserAbsent を発火します");
            _gameEventPublisher.PublishUserAbsent();
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
