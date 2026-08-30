using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
    /// <summary>
    /// Domain-only fail-closed checks for v2 compatibility, enum mapping and selected
    /// artifact readiness. No endpoint, auth, or download behavior is implemented here.
    /// </summary>
    public static class YummyServiceV2ContractGuard
    {
        public static bool TryValidateCompatibility(
            YummyServiceV2Compatibility compatibility,
            out ContractGuardFailure failure)
        {
            if (compatibility == null)
            {
                failure = Failure(
                    ContractGuardFailureCode.MissingRequiredField,
                    null,
                    string.Empty,
                    string.Empty,
                    "compatibility",
                    "v2 compatibility metadata is required.");
                return false;
            }

            if (compatibility.LooksLikeV1)
            {
                failure = Failure(
                    ContractGuardFailureCode.V1Rejected,
                    compatibility,
                    "v1 contract/route/response is not accepted by the v2 boundary.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(compatibility.RepositoryCommit)
                || string.IsNullOrWhiteSpace(compatibility.OpenApiVersion)
                || string.IsNullOrWhiteSpace(compatibility.OpenApiSha256))
            {
                failure = Failure(
                    ContractGuardFailureCode.MissingRequiredField,
                    compatibility,
                    "v2 compatibility metadata is incomplete.");
                return false;
            }

            if (!string.Equals(compatibility.OpenApiVersion, YummyServiceV2Contract.OpenApiVersion, StringComparison.Ordinal))
            {
                failure = Failure(
                    ContractGuardFailureCode.ContractVersionMismatch,
                    compatibility,
                    "the deployed contract version is not the reviewed v2 contract.");
                return false;
            }

            if (!string.Equals(compatibility.RepositoryCommit, YummyServiceV2Contract.RepositoryCommit, StringComparison.Ordinal))
            {
                failure = Failure(
                    ContractGuardFailureCode.ContractRevisionMismatch,
                    compatibility,
                    "the deployed repository revision is not the reviewed v2 contract.");
                return false;
            }

            if (!string.Equals(compatibility.OpenApiSha256, YummyServiceV2Contract.OpenApiSha256, StringComparison.OrdinalIgnoreCase))
            {
                failure = Failure(
                    ContractGuardFailureCode.ContractChecksumMismatch,
                    compatibility,
                    "the deployed OpenAPI checksum is not the reviewed v2 contract.");
                return false;
            }

            failure = null;
            return true;
        }

        public static bool TryValidateCompatibility(YummyServiceV2Compatibility compatibility)
        {
            return TryValidateCompatibility(compatibility, out _);
        }

        public static bool TryAcceptSelectedGlb(
            OrderState orderState,
            ArtifactRef? selectedGlb,
            string serverUrl,
            string operation,
            out ContractGuardFailure failure)
        {
            if (orderState != OrderState.Completed)
            {
                failure = Failure(
                    ContractGuardFailureCode.NotSelectable,
                    serverUrl,
                    string.Empty,
                    operation,
                    orderState.ToString(),
                    "only a COMPLETED order can be selected.");
                return false;
            }

            if (!selectedGlb.HasValue || !selectedGlb.Value.IsValid)
            {
                failure = Failure(
                    ContractGuardFailureCode.InvalidArtifact,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Glb.ToString(),
                    "a selected valid GLB artifact is required.");
                return false;
            }

            if (selectedGlb.Value.ArtifactType != ArtifactType.Glb)
            {
                failure = Failure(
                    ContractGuardFailureCode.WrongArtifactType,
                    serverUrl,
                    string.Empty,
                    operation,
                    selectedGlb.Value.ArtifactType.ToString(),
                    "the selected model artifact must have type GLB.");
                return false;
            }

            if (!selectedGlb.Value.Verified)
            {
                failure = Failure(
                    ContractGuardFailureCode.ArtifactNotVerified,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Glb.ToString(),
                    "an unverified GLB artifact is not selectable.");
                return false;
            }

            failure = null;
            return true;
        }

        /// <summary>
        /// Completes the client-side integrity gate for a downloaded model. Metadata
        /// readiness and byte integrity are separate checks so a server-side
        /// <c>verified</c> flag cannot stand in for hashing the received bytes.
        /// </summary>
        public static bool TryAcceptDownloadedGlb(
            OrderState orderState,
            ArtifactRef? selectedGlb,
            byte[] downloadedBytes,
            string serverUrl,
            string operation,
            out ContractGuardFailure failure)
        {
            if (!TryAcceptSelectedGlb(orderState, selectedGlb, serverUrl, operation, out failure))
            {
                return false;
            }

            if (!selectedGlb.Value.VerifyBytes(downloadedBytes))
            {
                failure = Failure(
                    ContractGuardFailureCode.ArtifactIntegrityMismatch,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Glb.ToString(),
                    "downloaded bytes did not match the selected artifact SHA-256.");
                return false;
            }

            failure = null;
            return true;
        }

        /// <summary>
        /// 咀嚼音として selected WAV を受け入れてよいかを判定する。
        ///
        /// Device の status projection は <c>wav.downloadable</c> が true のときだけ
        /// <c>artifact_id</c> を返す。false のときに ID を推測して download してはならないため、
        /// GLB と同じく COMPLETED / 型一致 / verified の3点で fail closed にする。
        /// </summary>
        public static bool TryAcceptSelectedWav(
            OrderState orderState,
            ArtifactRef? selectedWav,
            string serverUrl,
            string operation,
            out ContractGuardFailure failure)
        {
            if (orderState != OrderState.Completed)
            {
                failure = Failure(
                    ContractGuardFailureCode.NotSelectable,
                    serverUrl,
                    string.Empty,
                    operation,
                    orderState.ToString(),
                    "only a COMPLETED order can provide a chewing sound.");
                return false;
            }

            if (!selectedWav.HasValue || !selectedWav.Value.IsValid)
            {
                failure = Failure(
                    ContractGuardFailureCode.InvalidArtifact,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Wav.ToString(),
                    "a selected valid WAV artifact is required.");
                return false;
            }

            if (selectedWav.Value.ArtifactType != ArtifactType.Wav)
            {
                failure = Failure(
                    ContractGuardFailureCode.WrongArtifactType,
                    serverUrl,
                    string.Empty,
                    operation,
                    selectedWav.Value.ArtifactType.ToString(),
                    "the selected chewing sound artifact must have type WAV.");
                return false;
            }

            if (!selectedWav.Value.Verified)
            {
                failure = Failure(
                    ContractGuardFailureCode.ArtifactNotVerified,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Wav.ToString(),
                    "an unverified WAV artifact is not playable.");
                return false;
            }

            failure = null;
            return true;
        }

        /// <summary>
        /// 受信した咀嚼音 bytes の整合性まで確かめる。
        ///
        /// 現行の Unity Device contract では status も download response も sha256 を返さない
        /// (contract gap)。checksum を得られるようになるまで、このゲートは通せない。
        /// </summary>
        public static bool TryAcceptDownloadedWav(
            OrderState orderState,
            ArtifactRef? selectedWav,
            byte[] downloadedBytes,
            string serverUrl,
            string operation,
            out ContractGuardFailure failure)
        {
            if (!TryAcceptSelectedWav(orderState, selectedWav, serverUrl, operation, out failure))
            {
                return false;
            }

            if (!selectedWav.Value.VerifyBytes(downloadedBytes))
            {
                failure = Failure(
                    ContractGuardFailureCode.ArtifactIntegrityMismatch,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Wav.ToString(),
                    "downloaded bytes did not match the selected artifact SHA-256.");
                return false;
            }

            failure = null;
            return true;
        }

        public static bool TryAcceptSelectedWav(
            GeneratedFoodItem item,
            string serverUrl,
            string operation,
            out ContractGuardFailure failure)
        {
            if (item == null || !item.IsValid)
            {
                failure = Failure(
                    ContractGuardFailureCode.NotSelectable,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Wav.ToString(),
                    "the generated food item is invalid or incomplete.");
                return false;
            }

            if (!item.TryGetSelectedWav(out var wav))
            {
                failure = Failure(
                    ContractGuardFailureCode.InvalidArtifact,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Wav.ToString(),
                    "the generated food item has no selected WAV artifact.");
                return false;
            }

            return TryAcceptSelectedWav(item.OrderState, wav, serverUrl, operation, out failure);
        }

        public static bool TryAcceptSelectedGlb(
            GeneratedFoodItem item,
            string serverUrl,
            string operation,
            out ContractGuardFailure failure)
        {
            if (item == null || !item.IsValid)
            {
                failure = Failure(
                    ContractGuardFailureCode.NotSelectable,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Glb.ToString(),
                    "the generated food item is invalid or incomplete.");
                return false;
            }

            if (!item.TryGetSelectedGlb(out var glb))
            {
                failure = Failure(
                    ContractGuardFailureCode.InvalidArtifact,
                    serverUrl,
                    string.Empty,
                    operation,
                    ArtifactType.Glb.ToString(),
                    "the generated food item has no selected GLB artifact.");
                return false;
            }

            if (!TryAcceptSelectedGlb(item.OrderState, glb, serverUrl, operation, out failure))
            {
                return false;
            }

            if (!item.IsSelectable)
            {
                failure = Failure(
                    ContractGuardFailureCode.NotSelectable,
                    serverUrl,
                    string.Empty,
                    operation,
                    item.OrderState.ToWireValue(),
                    "the order state is complete but the five-stage completion gate is not satisfied.");
                return false;
            }

            failure = null;
            return true;
        }

        public static bool TryParseOrderState(string value, out OrderState state)
        {
            switch (value)
            {
                case "DRAFT": state = OrderState.Draft; return true;
                case "QUEUED": state = OrderState.Queued; return true;
                case "PROCESSING": state = OrderState.Processing; return true;
                case "AWAITING_ADMIN_REVIEW": state = OrderState.AwaitingAdminReview; return true;
                case "COMPLETED": state = OrderState.Completed; return true;
                case "REJECTED": state = OrderState.Rejected; return true;
                case "FAILED": state = OrderState.Failed; return true;
                case "CANCELED": state = OrderState.Canceled; return true;
                default: state = OrderState.Unknown; return false;
            }
        }

        public static bool TryParseStageState(string value, out StageState state)
        {
            switch (value)
            {
                case "PENDING": state = StageState.Pending; return true;
                case "QUEUED": state = StageState.Queued; return true;
                case "PROCESSING": state = StageState.Processing; return true;
                case "COMPLETED": state = StageState.Completed; return true;
                case "COMPLETED_WITH_WARNING": state = StageState.CompletedWithWarning; return true;
                case "FAILED": state = StageState.Failed; return true;
                case "CANCELED": state = StageState.Canceled; return true;
                default: state = StageState.Unknown; return false;
            }
        }

        public static bool TryParseStageType(string value, out StageType stageType)
        {
            switch (value)
            {
                case "INPUT_MODERATION": stageType = StageType.InputModeration; return true;
                case "EXAMPLE_RETRIEVAL": stageType = StageType.ExampleRetrieval; return true;
                case "FOOD_ANALYSIS": stageType = StageType.FoodAnalysis; return true;
                case "IMAGE_TO_3D": stageType = StageType.ImageTo3D; return true;
                case "AUDIO_GENERATION": stageType = StageType.AudioGeneration; return true;
                default: stageType = StageType.Unknown; return false;
            }
        }

        public static bool TryParseArtifactType(string value, out ArtifactType artifactType)
        {
            switch (value)
            {
                case "SOURCE_IMAGE_ORIGINAL": artifactType = ArtifactType.SourceImageOriginal; return true;
                case "SOURCE_IMAGE_NORMALIZED": artifactType = ArtifactType.SourceImageNormalized; return true;
                case "FOOD_ANALYSIS_JSON": artifactType = ArtifactType.FoodAnalysisJson; return true;
                case "GLB": artifactType = ArtifactType.Glb; return true;
                case "WAV": artifactType = ArtifactType.Wav; return true;
                default: artifactType = ArtifactType.Unknown; return false;
            }
        }

        public static bool TryParseModerationStatus(string value, out ModerationStatus status)
        {
            switch (value)
            {
                case "PASS": status = ModerationStatus.Pass; return true;
                case "REVIEW": status = ModerationStatus.Review; return true;
                case "BLOCK": status = ModerationStatus.Block; return true;
                default: status = ModerationStatus.Unknown; return false;
            }
        }

        public static bool TryParseFoodAnalysisStatus(string value, out FoodAnalysisStatus status)
        {
            switch (value)
            {
                case "VALID": status = FoodAnalysisStatus.Valid; return true;
                case "REVIEW_REQUIRED": status = FoodAnalysisStatus.ReviewRequired; return true;
                default: status = FoodAnalysisStatus.Unknown; return false;
            }
        }

        public static bool TryParseFoodAnalysisAdminDecision(string value, out FoodAnalysisAdminDecision decision)
        {
            switch (value)
            {
                case "APPROVED": decision = FoodAnalysisAdminDecision.Approved; return true;
                case "REVIEW": decision = FoodAnalysisAdminDecision.Review; return true;
                default: decision = FoodAnalysisAdminDecision.Unknown; return false;
            }
        }

        public static bool TryParseOrderState(string value, out OrderState state, out ContractGuardFailure failure)
        {
            if (TryParseOrderState(value, out state))
            {
                failure = null;
                return true;
            }

            failure = UnknownEnumFailure(value, "OrderState");
            return false;
        }

        public static bool TryParseStageState(string value, out StageState state, out ContractGuardFailure failure)
        {
            if (TryParseStageState(value, out state))
            {
                failure = null;
                return true;
            }

            failure = UnknownEnumFailure(value, "StageState");
            return false;
        }

        public static bool TryParseArtifactType(string value, out ArtifactType artifactType, out ContractGuardFailure failure)
        {
            if (TryParseArtifactType(value, out artifactType))
            {
                failure = null;
                return true;
            }

            failure = UnknownEnumFailure(value, "ArtifactType");
            return false;
        }

        public static bool TryParseStageType(string value, out StageType stageType, out ContractGuardFailure failure)
        {
            if (TryParseStageType(value, out stageType))
            {
                failure = null;
                return true;
            }

            failure = UnknownEnumFailure(value, "StageType");
            return false;
        }

        public static bool TryParseModerationStatus(string value, out ModerationStatus status, out ContractGuardFailure failure)
        {
            if (TryParseModerationStatus(value, out status))
            {
                failure = null;
                return true;
            }

            failure = UnknownEnumFailure(value, "ModerationStatus");
            return false;
        }

        public static bool TryParseFoodAnalysisStatus(string value, out FoodAnalysisStatus status, out ContractGuardFailure failure)
        {
            if (TryParseFoodAnalysisStatus(value, out status))
            {
                failure = null;
                return true;
            }

            failure = UnknownEnumFailure(value, "FoodAnalysisStatus");
            return false;
        }

        public static bool TryParseFoodAnalysisAdminDecision(
            string value,
            out FoodAnalysisAdminDecision decision,
            out ContractGuardFailure failure)
        {
            if (TryParseFoodAnalysisAdminDecision(value, out decision))
            {
                failure = null;
                return true;
            }

            failure = UnknownEnumFailure(value, "FoodAnalysisAdminDecision");
            return false;
        }

        /// <summary>
        /// Local negative fixture helper. It only identifies and rejects legacy v1
        /// markers; it never sends a request or performs a fallback.
        /// </summary>
        public static bool ContainsV1Marker(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var lower = value.Trim().ToLowerInvariant();
            var compact = lower.Replace(" ", string.Empty).Replace("_", string.Empty);
            return lower == "v1"
                   || lower.Contains("/v1/")
                   || lower.Contains("/v1?")
                   || lower.Contains("/v1#")
                   || lower.EndsWith("/v1", StringComparison.Ordinal)
                   || lower.StartsWith("v1/", StringComparison.Ordinal)
                   || lower.StartsWith("v1.", StringComparison.Ordinal)
                   || lower.StartsWith("v1?", StringComparison.Ordinal)
                   || lower.StartsWith("v1#", StringComparison.Ordinal)
                   || compact.Contains("\"version\":\"v1")
                   || compact.Contains("\"apiversion\":\"v1");
        }

        internal static string SanitizeServerUrl(string serverUrl)
        {
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri))
            {
                // Query/fragment commonly carry tokens or signed URLs. Do not use
                // Uri.GetLeftPart here because it may retain userinfo (including a
                // password) in the authority. Build a credential-free origin/path.
                var host = uri.Host;
                if (host.IndexOf(':') >= 0 && !host.StartsWith("[", StringComparison.Ordinal))
                {
                    host = string.Concat("[", host, "]");
                }

                var port = uri.IsDefaultPort ? string.Empty : string.Concat(":", uri.Port);
                return string.Concat(uri.Scheme, "://", host, port, uri.AbsolutePath);
            }

            return "[invalid-server-url]";
        }

        internal static string SanitizeOperation(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                return string.Empty;
            }

            var value = operation.Trim();
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                return SanitizeServerUrl(value);
            }

            var queryStart = value.IndexOf('?');
            var fragmentStart = value.IndexOf('#');
            var cutAt = queryStart < 0
                ? fragmentStart
                : fragmentStart < 0 ? queryStart : Math.Min(queryStart, fragmentStart);
            return cutAt < 0 ? value : value.Substring(0, cutAt);
        }

        private static ContractGuardFailure UnknownEnumFailure(string value, string enumName)
        {
            return Failure(
                ContractGuardFailureCode.UnknownEnum,
                null,
                string.Empty,
                string.Empty,
                string.Concat(enumName, ":", value ?? "<null>"),
                "unknown v2 enum value was rejected; it cannot be mapped to a successful state.");
        }

        private static ContractGuardFailure Failure(
            ContractGuardFailureCode code,
            string serverUrl,
            string contractRevision,
            string operation,
            string stateOrType,
            string message)
        {
            return new ContractGuardFailure(code, serverUrl, contractRevision, operation, stateOrType, message);
        }

        private static ContractGuardFailure Failure(
            ContractGuardFailureCode code,
            YummyServiceV2Compatibility compatibility,
            string message)
        {
            return Failure(
                code,
                compatibility == null ? null : compatibility.ServerUrl,
                compatibility == null ? string.Empty : compatibility.ContractRevision,
                compatibility == null ? string.Empty : compatibility.Operation,
                "contract",
                message);
        }
    }

}
