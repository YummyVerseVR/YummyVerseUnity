using Meta.XR.MRUtilityKit;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model.Struct
{
    public class MRUKTrackableAdapter : IQRTrackable
    {
        private readonly MRUKTrackable _inner;

        public MRUKTrackableAdapter(MRUKTrackable inner)
        {
            _inner = inner;
        }
        public Transform transform => _inner.transform;
        public string qrPayload => _inner.MarkerPayloadString;
    }
}