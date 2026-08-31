using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
    /// <summary>
    /// Explicit wire names keep enum mapping strict and audit-friendly.
    /// </summary>
    public static class YummyServiceV2EnumExtensions
    {
        public static string ToWireValue(this OrderState state)
        {
            switch (state)
            {
                case OrderState.Draft: return "DRAFT";
                case OrderState.Queued: return "QUEUED";
                case OrderState.Processing: return "PROCESSING";
                case OrderState.AwaitingAdminReview: return "AWAITING_ADMIN_REVIEW";
                case OrderState.Completed: return "COMPLETED";
                case OrderState.Rejected: return "REJECTED";
                case OrderState.Failed: return "FAILED";
                case OrderState.Canceled: return "CANCELED";
                default: return string.Empty;
            }
        }

        public static string ToWireValue(this StageState state)
        {
            switch (state)
            {
                case StageState.Pending: return "PENDING";
                case StageState.Queued: return "QUEUED";
                case StageState.Processing: return "PROCESSING";
                case StageState.Completed: return "COMPLETED";
                case StageState.CompletedWithWarning: return "COMPLETED_WITH_WARNING";
                case StageState.Failed: return "FAILED";
                case StageState.Canceled: return "CANCELED";
                default: return string.Empty;
            }
        }

        public static string ToWireValue(this StageType stageType)
        {
            switch (stageType)
            {
                case StageType.InputModeration: return "INPUT_MODERATION";
                case StageType.ExampleRetrieval: return "EXAMPLE_RETRIEVAL";
                case StageType.FoodAnalysis: return "FOOD_ANALYSIS";
                case StageType.ImageTo3D: return "IMAGE_TO_3D";
                case StageType.AudioGeneration: return "AUDIO_GENERATION";
                default: return string.Empty;
            }
        }

        public static string ToWireValue(this ArtifactType artifactType)
        {
            switch (artifactType)
            {
                case ArtifactType.SourceImageOriginal: return "SOURCE_IMAGE_ORIGINAL";
                case ArtifactType.SourceImageNormalized: return "SOURCE_IMAGE_NORMALIZED";
                case ArtifactType.FoodAnalysisJson: return "FOOD_ANALYSIS_JSON";
                case ArtifactType.Glb: return "GLB";
                case ArtifactType.Wav: return "WAV";
                default: return string.Empty;
            }
        }

        public static string ToWireValue(this ModerationStatus status)
        {
            switch (status)
            {
                case ModerationStatus.Pass: return "PASS";
                case ModerationStatus.Review: return "REVIEW";
                case ModerationStatus.Block: return "BLOCK";
                default: return string.Empty;
            }
        }

        public static string ToWireValue(this FoodAnalysisStatus status)
        {
            switch (status)
            {
                case FoodAnalysisStatus.Valid: return "VALID";
                case FoodAnalysisStatus.ReviewRequired: return "REVIEW_REQUIRED";
                default: return string.Empty;
            }
        }

        public static string ToWireValue(this FoodAnalysisAdminDecision decision)
        {
            switch (decision)
            {
                case FoodAnalysisAdminDecision.Approved: return "APPROVED";
                case FoodAnalysisAdminDecision.Review: return "REVIEW";
                default: return string.Empty;
            }
        }
    }
}
