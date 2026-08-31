using System;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    [Serializable]
    public struct FoodPlacementData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion;
        public string AnchorUuid;
        public bool HasFoodPose;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public bool TryGetAnchorUuid(out Guid uuid)
        {
            return Guid.TryParse(AnchorUuid, out uuid) && uuid != Guid.Empty;
        }

        public bool IsValid()
        {
            if (SchemaVersion != CurrentSchemaVersion || !TryGetAnchorUuid(out _)) return false;
            if (!HasFoodPose) return false;

            return IsFinite(LocalPosition.x)
                   && IsFinite(LocalPosition.y)
                   && IsFinite(LocalPosition.z)
                   && IsFinite(LocalRotation.x)
                   && IsFinite(LocalRotation.y)
                   && IsFinite(LocalRotation.z)
                   && IsFinite(LocalRotation.w)
                   && RotationSqrMagnitude(LocalRotation) > 0.000001f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float RotationSqrMagnitude(Quaternion rotation)
        {
            return rotation.x * rotation.x
                   + rotation.y * rotation.y
                   + rotation.z * rotation.z
                   + rotation.w * rotation.w;
        }
    }
}
