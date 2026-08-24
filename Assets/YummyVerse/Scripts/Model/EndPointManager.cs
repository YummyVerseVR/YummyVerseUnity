using System;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class EndPointManager : IEndPointManager
    {
        // YummyService v2 の production endpoint は未公開。旧 server を既定値として保持しない。
        public string baseEndPointUrl { get; private set; } = string.Empty;

        public bool UpdateEndPointUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
                uriResult.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            baseEndPointUrl = uriResult.ToString();
            return true;
        }
    }
}
