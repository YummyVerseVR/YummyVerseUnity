using UnityEngine;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IQRTrackable
    {
        Transform transform { get; }
        string qrPayload { get; }
    }
}