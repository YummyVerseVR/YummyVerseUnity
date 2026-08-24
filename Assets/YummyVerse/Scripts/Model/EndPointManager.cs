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

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
            {
                return false;
            }

            // Production endpoint は HTTPS のみ。YummyApiMock の公式なローカル起動
            // (http://127.0.0.1:8010) はEditor統合確認用にloopbackだけ許可する。
            var isHttps = uriResult.Scheme == Uri.UriSchemeHttps;
            var isLoopbackHttp = uriResult.Scheme == Uri.UriSchemeHttp && uriResult.IsLoopback;
            if (!isHttps && !isLoopbackHttp) return false;

            baseEndPointUrl = uriResult.ToString();
            return true;
        }
    }
}
