using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class QRDetectionService : IQRDetectionService, IDisposable
    {
        public ReactiveProperty<Guid> OnChangeGUID { get; } = new();
        public ReactiveProperty<Transform> OnChangeTransform { get; }  = new();

        private readonly Subject<Unit> _onLost = new();
        public Observable<Unit> OnLost => _onLost;

        public void NotifyDetectQR(Guid guid, Transform transform)
        {
            OnChangeGUID.Value = guid; // Guidは値型なので、中身が変われば更新通知が飛ぶ。
            var previous = OnChangeTransform.Value;
            OnChangeTransform.Value = transform;
            if (ReferenceEquals(previous, transform))
            {
                OnChangeTransform.OnNext(transform);
            }
        }

        public void NotifyLostQR()
        {
            _onLost.OnNext(Unit.Default);
        }

        public void Reset()
        {
            OnChangeGUID.Value = Guid.Empty;
            OnChangeTransform.Value = null;
        }

        public void Dispose()
        {
            _onLost?.Dispose();
            OnChangeGUID?.Dispose();
            OnChangeTransform?.Dispose();
        }
    }
}
