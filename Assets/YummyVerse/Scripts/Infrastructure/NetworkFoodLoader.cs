using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>選択されたAPI v2食品のGLBだけを取得してglTFastへ渡す。</summary>
    public sealed class NetworkFoodLoader : INetworkFoodModelLoader
    {
        private readonly IEndPointManager _endPointManager;
        private readonly IYummyServiceV2Credentials _credentials;

        public NetworkFoodLoader(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager ?? throw new ArgumentNullException(nameof(endPointManager));
            _credentials = endPointManager as IYummyServiceV2Credentials;
        }

        public async UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken ct)
        {
            var result = new FoodDownloadResult
            {
                RequestedGuid = item.Guid,
                RequestedItemId = item.Id
            };

            if (item.Source != MenuItemSource.ApiV2 || !TryGetToken(out var token))
            {
                result.StatusCode = HttpStatusCode.BadRequest;
                return result;
            }

            if (!TryResolveArtifactUrl(item.OrderId, item.ModelArtifactId, out var modelUrl)
                || !Uri.TryCreate(modelUrl, UriKind.Absolute, out var modelUri)
                || (modelUri.Scheme != Uri.UriSchemeHttps && modelUri.Scheme != Uri.UriSchemeHttp))
            {
                result.StatusCode = HttpStatusCode.BadRequest;
                return result;
            }

            using var request = UnityWebRequest.Get(modelUrl);
            request.timeout = 30;
            request.SetRequestHeader("Accept", "model/gltf-binary");
            request.SetRequestHeader("Authorization", $"Bearer {token}");

            try
            {
                await request.SendWebRequest().WithCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnityWebRequestException)
            {
                result.StatusCode = request.responseCode > 0
                    ? (HttpStatusCode)request.responseCode
                    : 0;
                return result;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                result.StatusCode = request.responseCode > 0
                    ? (HttpStatusCode)request.responseCode
                    : 0;
                return result;
            }

            var contentType = request.GetResponseHeader("Content-Type");
            if (string.IsNullOrWhiteSpace(contentType)
                || !contentType.StartsWith("model/gltf-binary", StringComparison.OrdinalIgnoreCase))
            {
                result.StatusCode = HttpStatusCode.UnsupportedMediaType;
                return result;
            }

            var bytes = request.downloadHandler.data;
            if (bytes == null || bytes.Length == 0)
            {
                result.StatusCode = HttpStatusCode.NoContent;
                return result;
            }

            try
            {
                var cacheDirectory = Path.Combine(Application.temporaryCachePath, "YummyVerse", "Foods");
                Directory.CreateDirectory(cacheDirectory);
                var cachePath = Path.Combine(cacheDirectory, CreateCacheName(item) + ".glb");
                await File.WriteAllBytesAsync(cachePath, bytes, ct);

                var gltfImport = GltfImportFactory.Create();
                try
                {
                    var loaded = await gltfImport.Load(cachePath, cancellationToken: ct);
                    if (!loaded)
                    {
                        gltfImport.Dispose();
                        result.StatusCode = HttpStatusCode.InternalServerError;
                        return result;
                    }

                    result.StatusCode = HttpStatusCode.OK;
                    // 咀嚼音はモデルと同じ資格情報でエンドポイントから取得する。
                    // 取れなくても食品の表示は続けるため、失敗時は null のままにして
                    // 既定の咀嚼音へフォールバックさせる。
                    TryResolveArtifactUrl(item.OrderId, item.AudioArtifactId, out var audioUrl);
                    var chewSound = await ChewSoundLoader.LoadFromUrlAsync(
                        audioUrl, token, ct);
                    result.Food = new Food { GltfImport = gltfImport, ChewSound = chewSound };
                }
                catch
                {
                    gltfImport.Dispose();
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"API v2 food could not be cached: {exception.GetType().Name}");
                result.StatusCode = HttpStatusCode.InternalServerError;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"API v2 food could not be loaded: {exception.GetType().Name}");
                result.StatusCode = HttpStatusCode.InternalServerError;
            }

            return result;
        }

        private bool TryGetToken(out string token)
        {
            token = _credentials?.DeviceAccessToken;
            return !string.IsNullOrWhiteSpace(token);
        }

        private bool TryResolveArtifactUrl(string orderId, string artifactId, out string artifactUrl)
        {
            artifactUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(artifactId)) return false;
            return YummyServiceV2Url.TryBuildUnityDeviceArtifactDownloadUrl(
                _endPointManager.baseEndPointUrl,
                orderId,
                artifactId,
                out artifactUrl);
        }

        private static string CreateCacheName(MenuItem item)
        {
            using var sha256 = SHA256.Create();
            var value = string.Concat(
                item.OrderId, "\n", item.ModelArtifactId, "\n", item.Id, "\n", item.ModelLocation);
            var digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            var builder = new StringBuilder(digest.Length * 2);
            foreach (var valueByte in digest) builder.Append(valueByte.ToString("x2"));
            return builder.ToString();
        }
    }
}
