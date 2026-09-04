using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.YummyServiceV2;
using YummyVerse.Scripts.View.UI;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>Loads one preview image and leaves card/UI ownership to the caller.</summary>
    public sealed class FoodPreviewLoader
    {
        private readonly IEndPointManager _endPointManager;
        private readonly IYummyServiceV2Credentials _credentials;

        /// <summary>
        /// The endpoint is optional so local file previews remain usable in tests and
        /// offline mode. When configured, it is used only to decide whether a preview
        /// location is the authenticated Unity Device artifact route.
        /// </summary>
        public FoodPreviewLoader(IEndPointManager endPointManager = null)
        {
            _endPointManager = endPointManager;
            _credentials = endPointManager as IYummyServiceV2Credentials;
        }

        public async UniTask<Texture2D> LoadAsync(string location, CancellationToken cancellationToken)
        {
            var requestLocation = ResolveLocation(location);
            if (string.IsNullOrWhiteSpace(requestLocation)) return null;

            var isDeviceArtifact = _endPointManager != null
                                   && YummyServiceV2Url.IsSafeUnityDeviceArtifactDownloadUrl(
                                       _endPointManager.baseEndPointUrl,
                                       requestLocation);
            var deviceToken = string.Empty;
            if (isDeviceArtifact && !TryGetDeviceToken(out deviceToken)) return null;

            using var request = UnityWebRequest.Get(requestLocation);
            request.timeout = 15;
            if (isDeviceArtifact)
            {
                // Do not allow UnityWebRequest to follow a redirect after the
                // Authorization header is attached. The Device artifact endpoint is
                // expected to be final; refusing redirects keeps the token bound to
                // the configured origin even if a server or proxy is misconfigured.
                request.redirectLimit = 0;
                request.SetRequestHeader("Accept", "image/png");
                request.SetRequestHeader("Authorization", $"Bearer {deviceToken}");
            }

            try
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnityWebRequestException)
            {
                return null;
            }

            if (request.result != UnityWebRequest.Result.Success) return null;
            if (isDeviceArtifact)
            {
                var contentType = request.GetResponseHeader("Content-Type");
                if (string.IsNullOrWhiteSpace(contentType)
                    || !contentType.StartsWith("image/png", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }

            if (!FoodPreviewTextureDecoder.TryDecode(request.downloadHandler.data, out var texture)) return null;
            if (cancellationToken.IsCancellationRequested)
            {
                DestroyTexture(texture);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return texture;
        }

        private bool TryGetDeviceToken(out string token)
        {
            token = _credentials?.DeviceAccessToken;
            return !string.IsNullOrWhiteSpace(token);
        }

        private static string ResolveLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return string.Empty;
            if (Uri.TryCreate(location, UriKind.Absolute, out _)) return location;

            try
            {
                return new Uri(Path.GetFullPath(location)).AbsoluteUri;
            }
            catch (Exception exception) when (
                exception is ArgumentException
                or UriFormatException
                or NotSupportedException)
            {
                return string.Empty;
            }
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
            else UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
