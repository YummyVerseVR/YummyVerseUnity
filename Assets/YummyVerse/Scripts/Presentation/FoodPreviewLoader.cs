using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using YummyVerse.Scripts.Model.YummyServiceV2;
using YummyVerse.Scripts.View.UI;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>Loads one preview image and leaves card/UI ownership to the caller.</summary>
    public sealed class FoodPreviewLoader
    {
        public async UniTask<Texture2D> LoadAsync(string location, CancellationToken cancellationToken)
        {
            var requestLocation = ResolveLocation(location);
            if (string.IsNullOrWhiteSpace(requestLocation)) return null;

            using var request = UnityWebRequest.Get(requestLocation);
            request.timeout = 15;
            if (Uri.TryCreate(requestLocation, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                request.SetRequestHeader(
                    "Authorization",
                    $"Bearer {YummyServiceV2Url.DevelopmentAdminToken}");
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
            if (!FoodPreviewTextureDecoder.TryDecode(request.downloadHandler.data, out var texture)) return null;
            if (cancellationToken.IsCancellationRequested)
            {
                DestroyTexture(texture);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return texture;
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
