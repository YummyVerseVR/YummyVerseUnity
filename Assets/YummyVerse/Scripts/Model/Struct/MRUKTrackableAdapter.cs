using Meta.XR.MRUtilityKit;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model.Struct
{
    public class MRUKTrackableAdapter : IQRTrackable
    {
        public Transform transform { get; }
        public string qrPayload { get; }

        public MRUKTrackableAdapter(Transform transform, string payload)
        {
            this.transform = transform;
            qrPayload = payload;
        }
        
        public MRUKTrackableAdapter(MRUKTrackable trackable)
        {
            this.transform = trackable.transform;
            qrPayload = trackable.MarkerPayloadString;
        } 

    }
}