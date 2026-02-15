using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class QRDetectionService : IQRDetectionService
    {
        public ReactiveProperty<Guid> OnChangeGUID { get; } = new();
        public ReactiveProperty<Transform> OnChangeTransform { get; }  = new();

        public void NotifyDetectQR(Guid guid, Transform transform)
        {
            OnChangeGUID.Value = guid; // Guidは値型なので、中身が変われば更新通知が飛ぶ。
            OnChangeTransform.OnNext(transform); // Transformは参照型なので、毎回強制的に更新通知する(中身更新毎に更新通知したほうが重い気がする)
        }
    }
}