using UnityEngine;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model.Dummies.Struct
{
    public class DummyQRTrackable :  IQRTrackable
    {
        public DummyQRTrackable(Transform transform,  string qrPayload)
        {
            this.transform = transform;
            this.qrPayload = qrPayload;
        }

        public Transform transform { get; }

        public string qrPayload { get; }
    }
}