using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
    /// <summary>
    /// Constants for the reviewed YummyService v2 contract snapshot (ru322/main).
    ///
    /// The snapshot this pins supersedes the earlier main@546b455 snapshot, whose
    /// OpenAPI had no callable paths. Endpoint construction lives in
    /// <see cref="YummyServiceV2Url"/>; authentication and token handling stay out of
    /// this class so a token can never be pinned as a contract constant.
    /// </summary>
    public static class YummyServiceV2Contract
    {
        public const string Repository = "YummyVerseVR/YummyService";
        public const string RepositoryBranch = "ru322/main";
        public const string RepositoryCommit = "97c9ed75980ec398fe75159bd4e011b489112433";
        public const string OpenApiPath = "contracts/v2/openapi.yaml";
        public const string OpenApiVersion = "2.0.0-draft";
        public const string OpenApiSha256 = "18462aa900a6b031438635fd46ddc997746c9782d6476247b1cb82c011409616";
        public const string ReadmePath = "contracts/v2/README.md";
        public const string ReadmeSha256 = "0a0ec28cae5a1607df8ff4b6dab379324d5d1b18255ed52b6feb3c79c94f199d";
    }

    /// <summary>
    /// v2 order states. Unknown is a local sentinel only; it is never accepted as a
    /// wire value and is intentionally non-selectable.

    /// </summary>
    public enum OrderState
    {
        Unknown = 0,
        Draft = 1,
        Queued = 2,
        Processing = 3,
        AwaitingAdminReview = 4,
        Completed = 5,
        Rejected = 6,
        Failed = 7,
        Canceled = 8
    }

    /// <summary>
    /// v2 stage states. Unknown is a local sentinel only.

    /// </summary>
    public enum StageState
    {
        Unknown = 0,
        Pending = 1,
        Queued = 2,
        Processing = 3,
        Completed = 4,
        CompletedWithWarning = 5,
        Failed = 6,
        Canceled = 7
    }

    /// <summary>
    /// The five workflow stages defined by the v2 contract.

    /// </summary>
    public enum StageType
    {
        Unknown = 0,
        InputModeration = 1,
        ExampleRetrieval = 2,
        FoodAnalysis = 3,
        ImageTo3D = 4,
        AudioGeneration = 5
    }

    /// <summary>
    /// Moderation decision vocabulary from the v2 domain contract.

    /// </summary>
    public enum ModerationStatus
    {
        Unknown = 0,
        Pass = 1,
        Review = 2,
        Block = 3
    }

    /// <summary>
    /// Food Analysis schema/rule status vocabulary from v2.

    /// </summary>
    public enum FoodAnalysisStatus
    {
        Unknown = 0,
        Valid = 1,
        ReviewRequired = 2
    }

    /// <summary>
    /// Food Analysis administrator decision vocabulary from v2.

    /// </summary>
    public enum FoodAnalysisAdminDecision
    {
        Unknown = 0,
        Approved = 1,
        Review = 2
    }

    /// <summary>
    /// Immutable artifact kinds accepted by YummyService v2.

    /// </summary>
    public enum ArtifactType
    {
        Unknown = 0,
        SourceImageOriginal = 1,
        SourceImageNormalized = 2,
        FoodAnalysisJson = 3,
        Glb = 4,
        Wav = 5
    }

}
