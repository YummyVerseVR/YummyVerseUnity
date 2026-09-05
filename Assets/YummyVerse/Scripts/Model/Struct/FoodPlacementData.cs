using System;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 保存された食品の置き場所。姿勢は必ず「基準フレームから見た相対」で持つ。
    /// どの基準で測ったかを <see cref="ReferenceFrame"/> に記録しておかないと、
    /// 別の基準で測った値を取り違えて、現実とずれた場所に食品を出してしまう。
    /// </summary>
    [Serializable]
    public struct FoodPlacementData
    {
        /// <summary>
        /// v1 は Meta Spatial Anchor 基準、v2 は Stage 基準だった。どちらも
        /// この構成では意味のある位置を復元できなかったため、古い保存は読み捨てて
        /// 設定し直してもらう。<see cref="ReferenceFrame"/> も併せて検証すること。
        /// </summary>
        public const int CurrentSchemaVersion = 3;

        public int SchemaVersion;

        /// <summary>基準フレームの種類 (<c>IPlacementReferenceFrame.Kind</c> と同じ値)。</summary>
        public string ReferenceFrame;

        /// <summary>保存時の基準の世代 (<c>IPlacementReferenceFrame.GenerationId</c>)。</summary>
        public string FrameGenerationId;

        public bool HasFoodPose;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public bool IsValid()
        {
            if (SchemaVersion != CurrentSchemaVersion) return false;
            if (string.IsNullOrEmpty(ReferenceFrame)) return false;
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

        /// <summary>いま使える基準フレームで測った値かどうか。違う基準の値は使ってはいけない。</summary>
        public bool MatchesFrame(string frameKind, string generationId)
        {
            if (string.IsNullOrEmpty(frameKind)) return false;
            if (!string.Equals(ReferenceFrame, frameKind, StringComparison.Ordinal)) return false;

            // 世代が変わっていたら、同じ種類の基準でも別の物理位置を指している。
            return string.Equals(FrameGenerationId ?? string.Empty, generationId ?? string.Empty,
                StringComparison.Ordinal);
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
