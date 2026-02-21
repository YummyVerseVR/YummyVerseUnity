using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IQRDetectionService
    {
        ReactiveProperty<Guid> OnChangeGUID { get; }
        ReactiveProperty<Transform> OnChangeTransform { get; }
        
        void NotifyDetectQR(Guid guid, Transform transform);
    }
}
