using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class QRDetectionService : IQRDetectionService
    {
        public ReactiveProperty<QRDetection> OnDetected { get; } = new();
        public void NotifyDetectQR(Guid guid, Transform transform)
        {
            QRDetection qrDetection = new()
            {
                guid = guid,
                transform = transform
            };
            OnDetected.Value = qrDetection;
        }
    }
}