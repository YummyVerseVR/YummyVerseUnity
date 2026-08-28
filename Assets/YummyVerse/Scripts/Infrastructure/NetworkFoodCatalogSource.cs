using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// HTTP adapter for the remote catalog. FoodCatalogService only sees the source
    /// contract and therefore remains usable in EditMode tests without UnityWebRequest.
    /// </summary>
    public sealed class NetworkFoodCatalogSource : IRemoteFoodCatalogSource
    {
        private readonly IEndPointManager _endPointManager;

        public NetworkFoodCatalogSource(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager ?? throw new ArgumentNullException(nameof(endPointManager));
        }

        public async UniTask<FoodCatalogSourceResult> LoadAsync(CancellationToken cancellationToken)
        {
            var baseUrl = _endPointManager.baseEndPointUrl;
            if (!YummyServiceV2Url.TryBuildMenuUrl(baseUrl, out var menuUrl))
            {
                return FoodCatalogSourceResult.Empty("API v2 のベースURLが未設定です。");
            }

            using var request = UnityWebRequest.Get(menuUrl);
            request.timeout = 15;
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader(
                "Authorization",
                $"Bearer {YummyServiceV2Url.DevelopmentAdminToken}");

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
                return FoodCatalogSourceResult.Empty(DescribeRequestFailure(request));
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return FoodCatalogSourceResult.Empty(DescribeRequestFailure(request));
            }

            MenuResponseDto response;
            try
            {
                response = JsonUtility.FromJson<MenuResponseDto>(request.downloadHandler.text);
            }
            catch (ArgumentException)
            {
                return FoodCatalogSourceResult.Empty("API v2 の応答JSONを解析できませんでした。");
            }

            if (response?.items == null)
            {
                return FoodCatalogSourceResult.Empty("API v2 の応答に items がありません。");
            }

            IReadOnlyList<FoodCatalogItem> items = FoodCatalogTransportMapper.ToCatalogItems(
                response,
                baseUrl);
            return new FoodCatalogSourceResult(items);
        }

        private static string DescribeRequestFailure(UnityWebRequest request)
        {
            if (request.responseCode > 0)
            {
                return $"API v2 の取得に失敗しました ({(HttpStatusCode)request.responseCode})。";
            }

            return "API v2 に接続できませんでした。";
        }
    }
}
