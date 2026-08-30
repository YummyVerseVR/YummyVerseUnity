using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
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

        /// <summary>
        /// 咀嚼音として使える WAV かどうか。GLB と同じく、selected かつ verified な
        /// artifact revision だけを再生候補にする。
        /// </summary>
        public bool IsVerifiedWav => IsVerifiedArtifact && ArtifactType == ArtifactType.Wav;

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

}
