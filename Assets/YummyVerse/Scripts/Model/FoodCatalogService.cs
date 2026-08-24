using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;

namespace YummyVerse.Scripts.Model
{
    public sealed class FoodCatalogService : IFoodCatalogService
    {
        private readonly IEndPointManager _endPointManager;

        public FoodCatalogService(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager;
        }

        public async UniTask<FoodCatalogLoadResult> LoadAsync(CancellationToken ct)
        {
            var items = new List<FoodCatalogItem>();
            var apiError = string.Empty;

            var baseUrl = _endPointManager.baseEndPointUrl;
            if (YummyServiceV2Url.TryBuildMenuUrl(baseUrl, out var menuUrl))
            {
                var apiResult = await LoadApiItemsAsync(menuUrl, baseUrl, ct);
                items.AddRange(apiResult.Items);
                apiError = apiResult.Error;
            }
            else
            {
                apiError = "API v2 のベースURLが未設定です。";
            }

            ct.ThrowIfCancellationRequested();
            var localRoot = Path.Combine(Application.persistentDataPath, "Foods");
            items.AddRange(PersistentFoodCatalogScanner.Scan(localRoot));

            return new FoodCatalogLoadResult(items, apiError);
        }

        private static async UniTask<ApiLoadResult> LoadApiItemsAsync(
            string menuUrl,
            string baseUrl,
            CancellationToken ct)
        {
            using var request = UnityWebRequest.Get(menuUrl);
            request.timeout = 15;
            request.SetRequestHeader("Accept", "application/json");
            // 現行 v2 read API が公開している development Admin credential。
            // production auth が追加された場合は設定境界へ移す。
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
                return ApiLoadResult.Failed(DescribeRequestFailure(request));
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return ApiLoadResult.Failed(DescribeRequestFailure(request));
            }

            MenuResponseDto response;
            try
            {
                response = JsonUtility.FromJson<MenuResponseDto>(request.downloadHandler.text);
            }
            catch (ArgumentException)
            {
                return ApiLoadResult.Failed("API v2 の応答JSONを解析できませんでした。");
            }

            if (response?.items == null)
            {
                return ApiLoadResult.Failed("API v2 の応答に items がありません。");
            }

            var items = new List<FoodCatalogItem>(response.items.Length);
            foreach (var dto in response.items)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.id) || string.IsNullOrWhiteSpace(dto.display_name))
                {
                    continue;
                }

                YummyServiceV2Url.TryResolveLocation(baseUrl, dto.thumbnail_url, out var previewUrl);
                YummyServiceV2Url.TryResolveLocation(baseUrl, dto.sample_glb_url, out var modelUrl);
                items.Add(new FoodCatalogItem(
                    $"api-v2:{dto.id}",
                    dto.display_name,
                    previewUrl,
                    modelUrl,
                    MenuItemSource.ApiV2,
                    dto.available));
            }

            return new ApiLoadResult(items, string.Empty);
        }

        private static string DescribeRequestFailure(UnityWebRequest request)
        {
            if (request.responseCode > 0)
            {
                return $"API v2 の取得に失敗しました ({(HttpStatusCode)request.responseCode})。";
            }

            return "API v2 に接続できませんでした。";
        }

        [Serializable]
        private sealed class MenuResponseDto
        {
            public MenuItemDto[] items;
        }

        [Serializable]
        private sealed class MenuItemDto
        {
            public string id;
            public string display_name;
            public bool available;
            public string thumbnail_url;
            public string sample_glb_url;
        }

        private readonly struct ApiLoadResult
        {
            public ApiLoadResult(IReadOnlyList<FoodCatalogItem> items, string error)
            {
                Items = items;
                Error = error;
            }

            public IReadOnlyList<FoodCatalogItem> Items { get; }
            public string Error { get; }

            public static ApiLoadResult Failed(string error) =>
                new(Array.Empty<FoodCatalogItem>(), error);
        }
    }
}
