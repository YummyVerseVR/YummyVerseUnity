using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
    /// <summary>
    /// Constants for the reviewed YummyService v2 contract snapshot.
    ///
    /// This class deliberately contains no endpoint or authentication information. The
    /// v2 OpenAPI snapshot has no callable paths yet, so those concerns belong to a later
    /// transport contract and must not be inferred here.
    /// </summary>
    public static class YummyServiceV2Contract
    {
        public const string Repository = "YummyVerseVR/YummyService";
        public const string RepositoryCommit = "546b455fedd205fb686ca7b93d6af596bced7879";
        public const string OpenApiPath = "contracts/v2/openapi.yaml";
        public const string OpenApiVersion = "2.0.0-draft";
        public const string OpenApiSha256 = "73a21a380d23a136f92ddea7bc45cfcc9556aac040f5aa9a9e1c58d34ac0f5f0";
        public const string ReadmePath = "contracts/v2/README.md";
        public const string ReadmeSha256 = "e3f6635bf215b2e96b6005d8946fe5c6b0549f8db995efa11b2ac9139d91e46a";
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

    /// <summary>
    /// Stable, opaque generated-food/order identity.
    ///
    /// It intentionally is not a Guid and has no parsing or formatting behavior that
    /// would couple it to the legacy QR identity. The value is retained exactly as
    /// supplied by the v2 boundary.
    /// </summary>
    public readonly struct GeneratedFoodItemId : IEquatable<GeneratedFoodItemId>
    {
        private readonly string _value;

        public GeneratedFoodItemId(string value)
        {
            _value = value ?? string.Empty;
        }

        public string Value => _value ?? string.Empty;

        /// <summary>
        /// The v2 server order identity represented by this item identity.
        /// </summary>
        public string OrderIdentity => Value;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static bool TryCreate(string value, out GeneratedFoodItemId id)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                id = default(GeneratedFoodItemId);
                return false;
            }

            id = new GeneratedFoodItemId(value);
            return true;
        }

        public static GeneratedFoodItemId Create(string value)
        {
            if (!TryCreate(value, out var id))
            {
                throw new ArgumentException("GeneratedFoodItemId must be a non-empty opaque value.", nameof(value));
            }

            return id;
        }

        public bool Equals(GeneratedFoodItemId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GeneratedFoodItemId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString() => Value;

        public static bool operator ==(GeneratedFoodItemId left, GeneratedFoodItemId right) => left.Equals(right);
        public static bool operator !=(GeneratedFoodItemId left, GeneratedFoodItemId right) => !left.Equals(right);
    }

    /// <summary>
    /// Cache identity for an immutable artifact revision.
    /// </summary>
    public readonly struct ArtifactCacheIdentity : IEquatable<ArtifactCacheIdentity>
    {
        public ArtifactCacheIdentity(string artifactId, string revision, string sha256)
        {
            ArtifactId = artifactId ?? string.Empty;
            Revision = revision ?? string.Empty;
            Sha256 = sha256 ?? string.Empty;
        }

        public string ArtifactId { get; }
        public string Revision { get; }
        public string Sha256 { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ArtifactId)
                               && !string.IsNullOrWhiteSpace(Revision)
                               && ArtifactRef.IsSha256(Sha256);

        public bool Equals(ArtifactCacheIdentity other)
        {
            return string.Equals(ArtifactId, other.ArtifactId, StringComparison.Ordinal)
                   && string.Equals(Revision, other.Revision, StringComparison.Ordinal)
                   && string.Equals(Sha256, other.Sha256, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is ArtifactCacheIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(ArtifactId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Revision);
                hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Sha256);
                return hash;
            }
        }

        public override string ToString()
        {
            // Length-prefixing keeps opaque IDs/revisions unambiguous without treating
            // either value as a path or filename.
            return string.Concat(
                ArtifactId.Length.ToString(), ":", ArtifactId,
                ";", Revision.Length.ToString(), ":", Revision,
                ";", Sha256.ToLowerInvariant());
        }

        public static bool operator ==(ArtifactCacheIdentity left, ArtifactCacheIdentity right) => left.Equals(right);
        public static bool operator !=(ArtifactCacheIdentity left, ArtifactCacheIdentity right) => !left.Equals(right);
    }

    /// <summary>
    /// Immutable v2 artifact metadata. An unverified ref may be retained as metadata,
    /// but it can never be used as a preview/model candidate.
    /// </summary>
    public readonly struct ArtifactRef : IEquatable<ArtifactRef>
    {
        public ArtifactRef(string artifactId, ArtifactType artifactType, string revision, string sha256, bool verified)
        {
            ArtifactId = artifactId ?? string.Empty;
            ArtifactType = artifactType;
            Revision = revision ?? string.Empty;
            Sha256 = sha256 ?? string.Empty;
            Verified = verified;
        }

        public string ArtifactId { get; }
        public ArtifactType ArtifactType { get; }
        public string Revision { get; }
        public string Sha256 { get; }
        public bool Verified { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ArtifactId)
                               && ArtifactType != ArtifactType.Unknown
                               && Enum.IsDefined(typeof(ArtifactType), ArtifactType)
                               && !string.IsNullOrWhiteSpace(Revision)
                               && IsSha256(Sha256);

        public bool IsVerifiedArtifact => IsValid && Verified;

        public bool IsVerifiedGlb => IsVerifiedArtifact && ArtifactType == ArtifactType.Glb;

        public ArtifactCacheIdentity CacheIdentity => new ArtifactCacheIdentity(ArtifactId, Revision, Sha256);

        public static bool TryCreate(
            string artifactId,
            ArtifactType artifactType,
            string revision,
            string sha256,
            bool verified,
            out ArtifactRef artifact)
        {
            artifact = new ArtifactRef(artifactId, artifactType, revision, sha256, verified);
            return artifact.IsValid;
        }

        /// <summary>
        /// Checks the downloaded bytes against metadata. This is intentionally separate
        /// from <see cref="Verified"/>: a server-side verified flag never replaces the
        /// client-side byte hash check.
        /// </summary>
        public bool MatchesSha256(byte[] bytes)
        {
            if (!IsValid || bytes == null)
            {
                return false;
            }

            using (var sha256 = SHA256.Create())
            {
                var digest = sha256.ComputeHash(bytes);
                return string.Equals(ToHex(digest), Sha256, StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool VerifyBytes(byte[] bytes)
        {
            return IsVerifiedArtifact && MatchesSha256(bytes);
        }

        public bool Equals(ArtifactRef other)
        {
            return string.Equals(ArtifactId, other.ArtifactId, StringComparison.Ordinal)
                   && ArtifactType == other.ArtifactType
                   && string.Equals(Revision, other.Revision, StringComparison.Ordinal)
                   && string.Equals(Sha256, other.Sha256, StringComparison.OrdinalIgnoreCase)
                   && Verified == other.Verified;
        }

        public override bool Equals(object obj)
        {
            return obj is ArtifactRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(ArtifactId);
                hash = (hash * 397) ^ (int)ArtifactType;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Revision);
                hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Sha256);
                hash = (hash * 397) ^ Verified.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Concat(ArtifactId, "@", Revision, "[", ArtifactType, ",", Sha256, "] verified=", Verified);
        }

        internal static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                var isDigit = c >= '0' && c <= '9';
                var isLowerHex = c >= 'a' && c <= 'f';
                var isUpperHex = c >= 'A' && c <= 'F';
                if (!isDigit && !isLowerHex && !isUpperHex)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public static bool operator ==(ArtifactRef left, ArtifactRef right) => left.Equals(right);
        public static bool operator !=(ArtifactRef left, ArtifactRef right) => !left.Equals(right);
    }

    /// <summary>
    /// Application-domain representation of a generated order/menu item.
    ///
    /// It stores all five stage states and only the selected immutable artifact
    /// revisions. It never derives a selected revision from a latest/current artifact;
    /// selection must be explicit at the v2 boundary.
    /// </summary>
    public sealed class GeneratedFoodItem
    {
        private readonly IReadOnlyDictionary<StageType, StageState> _stageStates;
        private readonly IReadOnlyDictionary<ArtifactType, ArtifactRef> _selectedArtifacts;

        public GeneratedFoodItem(
            GeneratedFoodItemId id,
            OrderState orderState,
            IEnumerable<KeyValuePair<StageType, StageState>> stageStates,
            IEnumerable<KeyValuePair<ArtifactType, ArtifactRef>> selectedArtifacts)
        {
            Id = id;
            OrderState = orderState;
            _stageStates = CopyStageStates(stageStates);
            _selectedArtifacts = CopySelectedArtifacts(selectedArtifacts);
        }

        public GeneratedFoodItemId Id { get; }
        public string OrderIdentity => Id.OrderIdentity;
        public OrderState OrderState { get; }
        public IReadOnlyDictionary<StageType, StageState> StageStates => _stageStates;
        public IReadOnlyDictionary<ArtifactType, ArtifactRef> SelectedArtifacts => _selectedArtifacts;

        public bool HasAllStageStates
        {
            get
            {
                foreach (var stageType in KnownStageTypes)
                {
                    if (!_stageStates.ContainsKey(stageType) || _stageStates[stageType] == StageState.Unknown)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsValid
        {
            get
            {
                if (!Id.IsValid || !Enum.IsDefined(typeof(OrderState), OrderState) || OrderState == OrderState.Unknown)
                {
                    return false;
                }

                if (!HasSemanticallyValidStageStates())
                {
                    return false;
                }

                foreach (var selected in _selectedArtifacts)
                {
                    if (selected.Key == ArtifactType.Unknown
                        || !Enum.IsDefined(typeof(ArtifactType), selected.Key)
                        || selected.Value.ArtifactType != selected.Key
                        || !selected.Value.IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsSelectable => IsSelectableForMenu();

        public bool IsImageTo3DBranchIndependent =>
            GetStageState(StageType.ImageTo3D) == StageState.Pending
            || GetStageState(StageType.ImageTo3D) == StageState.Processing
            || GetStageState(StageType.ImageTo3D) == StageState.Queued
            || GetStageState(StageType.ImageTo3D) == StageState.Completed;

        public bool TryGetStageState(StageType stageType, out StageState state)
        {
            return _stageStates.TryGetValue(stageType, out state) && stageType != StageType.Unknown;
        }

        public StageState GetStageState(StageType stageType)
        {
            return TryGetStageState(stageType, out var state) ? state : StageState.Unknown;
        }

        public bool TryGetSelectedArtifact(ArtifactType artifactType, out ArtifactRef artifact)
        {
            return _selectedArtifacts.TryGetValue(artifactType, out artifact) && artifactType != ArtifactType.Unknown;
        }

        public bool TryGetSelectedGlb(out ArtifactRef artifact)
        {
            return TryGetSelectedArtifact(ArtifactType.Glb, out artifact);
        }

        /// <summary>
        /// The minimum ready gate from the consumer contract. This overload is useful
        /// at an adapter boundary before the complete order snapshot is assembled.
        /// </summary>
        public static bool MeetsMinimumReadyGate(OrderState orderState, ArtifactRef? selectedGlb)
        {
            return orderState == OrderState.Completed
                   && selectedGlb.HasValue
                   && selectedGlb.Value.IsVerifiedGlb;
        }

        private bool IsSelectableForMenu()
        {
            if (!IsValid || !MeetsMinimumReadyGate(OrderState, GetSelectedGlb()))
            {
                return false;
            }

            // A completed order is expected to satisfy the v2 completion gate. Keeping
            // this check local prevents malformed/missing stage responses from becoming
            // menu-ready through an optimistic order-level state alone.
            return GetStageState(StageType.InputModeration) == StageState.Completed
                   && (GetStageState(StageType.ExampleRetrieval) == StageState.Completed
                       || GetStageState(StageType.ExampleRetrieval) == StageState.CompletedWithWarning)
                   && GetStageState(StageType.FoodAnalysis) == StageState.Completed
                   && GetStageState(StageType.ImageTo3D) == StageState.Completed
                   && GetStageState(StageType.AudioGeneration) == StageState.Completed;
        }

        private ArtifactRef? GetSelectedGlb()
        {
            return TryGetSelectedGlb(out var artifact) ? artifact : (ArtifactRef?)null;
        }

        private bool HasSemanticallyValidStageStates()
        {
            if (!HasAllStageStates)
            {
                return false;
            }

            foreach (var stage in _stageStates)
            {
                if (!Enum.IsDefined(typeof(StageType), stage.Key)
                    || stage.Key == StageType.Unknown
                    || !Enum.IsDefined(typeof(StageState), stage.Value)
                    || stage.Value == StageState.Unknown)
                {
                    return false;
                }

                // Retrieval exhaustion/Zero Shot is the only confirmed warning outcome.
                if (stage.Value == StageState.CompletedWithWarning && stage.Key != StageType.ExampleRetrieval)
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyDictionary<StageType, StageState> CopyStageStates(
            IEnumerable<KeyValuePair<StageType, StageState>> source)
        {
            var copy = new Dictionary<StageType, StageState>();
            if (source != null)
            {
                foreach (var entry in source)
                {
                    copy[entry.Key] = entry.Value;
                }
            }

            return new ReadOnlyDictionary<StageType, StageState>(copy);
        }

        private static IReadOnlyDictionary<ArtifactType, ArtifactRef> CopySelectedArtifacts(
            IEnumerable<KeyValuePair<ArtifactType, ArtifactRef>> source)
        {
            var copy = new Dictionary<ArtifactType, ArtifactRef>();
            if (source != null)
            {
                foreach (var entry in source)
                {
                    copy[entry.Key] = entry.Value;
                }
            }

            return new ReadOnlyDictionary<ArtifactType, ArtifactRef>(copy);
        }

        private static readonly StageType[] KnownStageTypes =
        {
            StageType.InputModeration,
            StageType.ExampleRetrieval,
            StageType.FoodAnalysis,
            StageType.ImageTo3D,
            StageType.AudioGeneration
        };
    }

    /// <summary>
    /// Metadata used by the domain-side compatibility gate. It describes an already
    /// discovered contract; it does not perform HTTP discovery or choose a path.
    /// </summary>
    public sealed class YummyServiceV2Compatibility
    {
        public YummyServiceV2Compatibility(
            string serverUrl,
            string repositoryCommit,
            string openApiVersion,
            string openApiSha256,
            string operation = null)
        {
            ServerUrl = serverUrl ?? string.Empty;
            RepositoryCommit = repositoryCommit ?? string.Empty;
            OpenApiVersion = openApiVersion ?? string.Empty;
            OpenApiSha256 = openApiSha256 ?? string.Empty;
            Operation = operation ?? string.Empty;
        }

        public string ServerUrl { get; }
        public string RepositoryCommit { get; }
        public string OpenApiVersion { get; }
        public string ContractVersion => OpenApiVersion;
        public string OpenApiSha256 { get; }
        public string Operation { get; }

        public string ContractRevision => string.Concat(RepositoryCommit, "@", OpenApiVersion, "#", OpenApiSha256);

        public bool LooksLikeV1 => YummyServiceV2ContractGuard.ContainsV1Marker(ServerUrl)
                                   || YummyServiceV2ContractGuard.ContainsV1Marker(RepositoryCommit)
                                   || YummyServiceV2ContractGuard.ContainsV1Marker(OpenApiVersion)
                                   || YummyServiceV2ContractGuard.ContainsV1Marker(OpenApiSha256)
                                   || YummyServiceV2ContractGuard.ContainsV1Marker(Operation);

        public static YummyServiceV2Compatibility Expected(string serverUrl = "", string operation = "")
        {
            return new YummyServiceV2Compatibility(
                serverUrl,
                YummyServiceV2Contract.RepositoryCommit,
                YummyServiceV2Contract.OpenApiVersion,
                YummyServiceV2Contract.OpenApiSha256,
                operation);
        }
    }

    public enum ContractGuardFailureCode
    {
        None = 0,
        MissingRequiredField,
        V1Rejected,
        ContractVersionMismatch,
        ContractRevisionMismatch,
        ContractChecksumMismatch,
        UnknownEnum,
        InvalidArtifact,
        WrongArtifactType,
        ArtifactNotVerified,
        ArtifactIntegrityMismatch,
        NotSelectable
    }

    /// <summary>
    /// Secret-free diagnostic produced by a fail-closed contract guard.
    /// </summary>
    public sealed class ContractGuardFailure
    {
        public ContractGuardFailure(
            ContractGuardFailureCode code,
            string serverUrl,
            string contractRevision,
            string operation,
            string stateOrType,
            string message)
        {
            Code = code;
            ServerUrl = YummyServiceV2ContractGuard.SanitizeServerUrl(serverUrl);
            ContractRevision = contractRevision ?? string.Empty;
            Operation = YummyServiceV2ContractGuard.SanitizeOperation(operation);
            StateOrType = stateOrType ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public ContractGuardFailureCode Code { get; }
        public string ServerUrl { get; }
        public string ContractRevision { get; }
        public string Operation { get; }
        public string StateOrType { get; }
        public string Message { get; }

        public override string ToString()
        {
            return string.Concat(Code, ": ", Message, " [operation=", Operation,
                ", state/type=", StateOrType, ", contract=", ContractRevision, "]");
        }
    }

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
