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

namespace YummyVerse.Scripts.Model
{
    /// <summary>選択されたAPI v2食品のGLBだけを取得してglTFastへ渡す。</summary>
    public sealed class NetworkFoodLoader : INetworkFoodModelLoader
    {
        public async UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken ct)
        {
            var result = new FoodDownloadResult
            {
                RequestedGuid = item.Guid,
                RequestedItemId = item.Id
            };

            if (item.Source != MenuItemSource.ApiV2 ||
                !Uri.TryCreate(item.ModelLocation, UriKind.Absolute, out var modelUri) ||
                (modelUri.Scheme != Uri.UriSchemeHttps && modelUri.Scheme != Uri.UriSchemeHttp))
            {
                result.StatusCode = HttpStatusCode.BadRequest;
                return result;
            }

            using var request = UnityWebRequest.Get(item.ModelLocation);
            request.timeout = 30;
            request.SetRequestHeader("Accept", "model/gltf-binary, application/octet-stream");
            request.SetRequestHeader("Authorization", $"Bearer {YummyServiceV2Url.DevelopmentAdminToken}");

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
                var loaded = await gltfImport.Load(cachePath, cancellationToken: ct);
                result.StatusCode = loaded ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                if (loaded)
                {
                    result.Food = new Food { GltfImport = gltfImport };
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

            return result;
        }

        private static string CreateCacheName(MenuItem item)
        {
            using var sha256 = SHA256.Create();
            var value = $"{item.Id}\n{item.ModelLocation}";
            var digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            var builder = new StringBuilder(digest.Length * 2);
            foreach (var valueByte in digest) builder.Append(valueByte.ToString("x2"));
            return builder.ToString();
        }
    }
}
