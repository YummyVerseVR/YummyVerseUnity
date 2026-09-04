using System;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
    public static class YummyServiceV2Url
    {
        /// <summary>Builds GET /v2/devices/unity/orders with the contract query fields.</summary>
        public static bool TryBuildUnityDeviceOrdersUrl(
            string configuredBaseUrl,
            string state,
            string query,
            string foodName,
            int limit,
            string cursor,
            out string url)
        {
            url = string.Empty;
            if (limit < 1 || limit > 100) return false;
            if (!IsOptionalSearchValueValid(query) || !IsOptionalSearchValueValid(foodName)) return false;

            if (!TryBuildV2Url(configuredBaseUrl, "devices/unity/orders", out var baseUrl)) return false;

            var parameters = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(state))
            {
                parameters.Add("state=" + Uri.EscapeDataString(state));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                parameters.Add("q=" + Uri.EscapeDataString(query));
            }

            if (!string.IsNullOrWhiteSpace(foodName))
            {
                parameters.Add("food_name=" + Uri.EscapeDataString(foodName));
            }

            parameters.Add("limit=" + limit);
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                parameters.Add("cursor=" + Uri.EscapeDataString(cursor));
            }

            url = baseUrl + "?" + string.Join("&", parameters);
            return true;
        }

        /// <summary>Builds GET /v2/devices/unity/orders/{order_id}.</summary>
        public static bool TryBuildUnityDeviceOrderStatusUrl(
            string configuredBaseUrl, string orderId, out string url)
        {
            return TryBuildUnityDeviceOrderUrl(configuredBaseUrl, orderId, string.Empty, out url);
        }

        /// <summary>Builds GET /v2/devices/unity/orders/{order_id}/payload.</summary>
        public static bool TryBuildUnityDevicePayloadUrl(
            string configuredBaseUrl, string orderId, out string url)
        {
            return TryBuildUnityDeviceOrderUrl(configuredBaseUrl, orderId, "payload", out url);
        }

        /// <summary>Builds POST /v2/devices/unity/orders/{order_id}/payload/ack.</summary>
        public static bool TryBuildUnityDevicePayloadAckUrl(
            string configuredBaseUrl, string orderId, out string url)
        {
            return TryBuildUnityDeviceOrderUrl(configuredBaseUrl, orderId, "payload/ack", out url);
        }

        /// <summary>
        /// 生成 order の artifact (canonical generated image / GLB / 咀嚼音の WAV)
        /// を取る Unity Device の download URL。
        ///
        /// artifactId は status の <c>generated_image.artifact_id</c> /
        /// <c>glb.artifact_id</c> / <c>wav.artifact_id</c> をそのまま渡す。
        /// downloadable が false のときに ID を組み立ててはならない。
        /// order_id / artifact_id は opaque なので、解釈せずそのままエスケープして載せる。
        /// </summary>
        public static bool TryBuildUnityDeviceArtifactDownloadUrl(
            string configuredBaseUrl, string orderId, string artifactId, out string url)
        {
            url = string.Empty;
            if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(artifactId)) return false;

            var path = string.Concat(
                "devices/unity/orders/", Uri.EscapeDataString(orderId),
                "/artifacts/", Uri.EscapeDataString(artifactId), "/download");
            return TryBuildV2Url(configuredBaseUrl, path, out url);
        }

        /// <summary>
        /// Returns whether a URL is an artifact download on the configured YummyService
        /// origin and its v2 Unity Device API path.
        ///
        /// This is deliberately stricter than a same-origin check. Device credentials
        /// may be attached only to the exact route shape produced by
        /// <see cref="TryBuildUnityDeviceArtifactDownloadUrl"/>; a public image URL,
        /// redirect target, or another endpoint must remain unauthenticated.
        /// </summary>
        public static bool IsSafeUnityDeviceArtifactDownloadUrl(
            string configuredBaseUrl, string candidateUrl)
        {
            if (!TryParseHttpUri(configuredBaseUrl, out var configuredUri)
                || !TryParseHttpUri(candidateUrl, out var candidateUri))
            {
                return false;
            }

            // A query/fragment can change endpoint semantics or carry data outside
            // the route contract. Artifact URLs built by this class never include
            // either component.
            if (!string.IsNullOrEmpty(candidateUri.Query)
                || !string.IsNullOrEmpty(candidateUri.Fragment)
                || !SameOrigin(configuredUri, candidateUri))
            {
                return false;
            }

            var versionPath = GetV2Path(configuredUri);
            var prefix = versionPath.TrimEnd('/') + "/devices/unity/orders/";
            var path = candidateUri.AbsolutePath;
            if (!path.StartsWith(prefix, StringComparison.Ordinal)) return false;

            var remainder = path.Substring(prefix.Length);
            const string artifactMarker = "/artifacts/";
            const string downloadSuffix = "/download";
            var markerIndex = remainder.IndexOf(artifactMarker, StringComparison.Ordinal);
            if (markerIndex <= 0 || !remainder.EndsWith(downloadSuffix, StringComparison.Ordinal)) return false;

            var artifactStart = markerIndex + artifactMarker.Length;
            var artifactLength = remainder.Length - artifactStart - downloadSuffix.Length;
            if (artifactLength <= 0) return false;

            // Raw slashes would introduce additional route segments. Escaped slashes
            // remain valid opaque order/artifact ID bytes because the URL builder
            // escapes IDs before placing them in the path.
            var orderSegment = remainder.Substring(0, markerIndex);
            var artifactSegment = remainder.Substring(artifactStart, artifactLength);
            return orderSegment.IndexOf('/') < 0
                   && artifactSegment.IndexOf('/') < 0
                   && orderSegment.IndexOf('\\') < 0
                   && artifactSegment.IndexOf('\\') < 0;
        }

        /// <summary>設定された base URL の下に、/v2 を1回だけ挟んで path を繋ぐ。</summary>
        private static bool TryBuildV2Url(string configuredBaseUrl, string path, out string url)
        {
            url = string.Empty;
            if (!TryParseHttpUri(configuredBaseUrl, out var baseUri)) return false;

            var root = baseUri.GetLeftPart(UriPartial.Authority);
            url = root + GetV2Path(baseUri) + "/" + path.TrimStart('/');
            return true;
        }

        private static bool TryParseHttpUri(string value, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(value)
                || !Uri.TryCreate(value, UriKind.Absolute, out var parsed)
                || (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp)
                || !string.IsNullOrWhiteSpace(parsed.UserInfo))
            {
                return false;
            }

            uri = parsed;
            return true;
        }

        private static string GetV2Path(Uri baseUri)
        {
            var basePath = baseUri.AbsolutePath.TrimEnd('/');
            return basePath.EndsWith("/v2", StringComparison.OrdinalIgnoreCase)
                ? basePath
                : basePath + "/v2";
        }

        private static bool SameOrigin(Uri left, Uri right)
        {
            return string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
                   && left.Port == right.Port;
        }

        private static bool TryBuildUnityDeviceOrderUrl(
            string configuredBaseUrl, string orderId, string suffix, out string url)
        {
            url = string.Empty;
            if (string.IsNullOrWhiteSpace(orderId)) return false;

            var path = string.Concat(
                "devices/unity/orders/", Uri.EscapeDataString(orderId));
            if (!string.IsNullOrWhiteSpace(suffix)) path += "/" + suffix.Trim('/');
            return TryBuildV2Url(configuredBaseUrl, path, out url);
        }

        private static bool IsOptionalSearchValueValid(string value)
        {
            return string.IsNullOrWhiteSpace(value) || (value.Length >= 1 && value.Length <= 100);
        }

        public static bool TryResolveLocation(string configuredBaseUrl, string location, out string resolved)
        {
            resolved = string.Empty;
            if (string.IsNullOrWhiteSpace(location)) return false;

            if (Uri.TryCreate(location, UriKind.Absolute, out var absolute) &&
                (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp))
            {
                resolved = absolute.ToString();
                return true;
            }

            if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri)) return false;
            if (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp) return false;

            if (location.StartsWith("/", StringComparison.Ordinal))
            {
                resolved = new Uri(new Uri(baseUri.GetLeftPart(UriPartial.Authority)), location).ToString();
                return true;
            }

            var root = configuredBaseUrl.EndsWith("/", StringComparison.Ordinal)
                ? configuredBaseUrl
                : configuredBaseUrl + "/";
            resolved = new Uri(new Uri(root), location).ToString();
            return true;
        }
    }
}
