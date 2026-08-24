using System;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
    public static class YummyServiceV2Url
    {
        public const string MenuPath = "admin/menu";
        public const string DevelopmentAdminToken = "admin-demo-token";

        public static bool TryBuildMenuUrl(string configuredBaseUrl, out string url)
        {
            url = string.Empty;
            if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri)) return false;
            if (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp) return false;

            var root = configuredBaseUrl.TrimEnd('/');
            var path = baseUri.AbsolutePath.TrimEnd('/');
            url = path.EndsWith("/v2", StringComparison.OrdinalIgnoreCase)
                ? $"{root}/{MenuPath}"
                : $"{root}/v2/{MenuPath}";
            return true;
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
