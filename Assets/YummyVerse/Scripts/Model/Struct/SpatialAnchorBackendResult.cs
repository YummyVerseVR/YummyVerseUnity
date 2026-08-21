using System;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    public readonly struct SpatialAnchorBackendResult
    {
        public bool Success { get; }
        public Guid Uuid { get; }
        public Transform AnchorTransform { get; }
        public string ErrorMessage { get; }

        private SpatialAnchorBackendResult(bool success, Guid uuid, Transform anchorTransform, string errorMessage)
        {
            Success = success;
            Uuid = uuid;
            AnchorTransform = anchorTransform;
            ErrorMessage = errorMessage;
        }

        public static SpatialAnchorBackendResult Succeeded(Guid uuid, Transform anchorTransform)
        {
            return new SpatialAnchorBackendResult(true, uuid, anchorTransform, string.Empty);
        }

        public static SpatialAnchorBackendResult Failed(string errorMessage)
        {
            return new SpatialAnchorBackendResult(false, Guid.Empty, null, errorMessage);
        }
    }
}
