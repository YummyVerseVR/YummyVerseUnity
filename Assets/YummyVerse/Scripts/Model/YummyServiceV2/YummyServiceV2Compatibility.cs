using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
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

}
