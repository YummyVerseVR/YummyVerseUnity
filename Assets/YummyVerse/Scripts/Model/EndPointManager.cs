using System;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class EndPointManager : IEndPointManager, IYummyServiceV2Credentials
    {
        private readonly IYummyServiceV2ConfigStore _configStore;
        private bool _isRestoringCache;

        // YummyService v2 の production endpoint は未公開。旧 server を既定値として保持しない。
        public string baseEndPointUrl { get; private set; } = string.Empty;

        // The token has no build-time default. It is supplied at runtime and cached
        // alongside the endpoint in Application.persistentDataPath.
        public string DeviceAccessToken { get; private set; } = string.Empty;

        public EndPointManager(IYummyServiceV2ConfigStore configStore = null)
        {
            _configStore = configStore;
            RestoreCache();
        }

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
            SaveCache();
            return true;
        }

        public bool UpdateDeviceAccessToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            token = token.Trim();
            // Bearer credentials are opaque, but whitespace would change the HTTP
            // header semantics and is never valid input for this configuration.
            for (var i = 0; i < token.Length; i++)
            {
                if (char.IsWhiteSpace(token[i]) || char.IsControl(token[i])) return false;
            }

            DeviceAccessToken = token;
            SaveCache();
            return true;
        }

        public void ClearDeviceAccessToken()
        {
            DeviceAccessToken = string.Empty;
            SaveCache();
        }

        private void RestoreCache()
        {
            if (_configStore == null
                || !_configStore.TryLoad(out var endpointUrl, out var deviceAccessToken)) return;

            _isRestoringCache = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(endpointUrl)) UpdateEndPointUrl(endpointUrl);
                if (!string.IsNullOrWhiteSpace(deviceAccessToken))
                {
                    UpdateDeviceAccessToken(deviceAccessToken);
                }
            }
            finally
            {
                _isRestoringCache = false;
            }
        }

        private void SaveCache()
        {
            if (_isRestoringCache) return;
            _configStore?.Save(baseEndPointUrl, DeviceAccessToken);
        }
    }
}
