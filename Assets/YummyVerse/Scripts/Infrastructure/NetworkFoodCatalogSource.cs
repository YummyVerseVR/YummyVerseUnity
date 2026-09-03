using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// Reads generated orders through the authenticated v2 Unity Device API.
    ///
    /// The old Admin menu endpoint is intentionally not used here.  The Device API
    /// returns a sanitized order projection, so the transport mapper derives the
    /// canonical generated-image preview and only selected downloadable GLB/WAV
    /// artifact IDs from the projection. All artifact URLs remain on the Device API
    /// route and are authenticated by their consuming loader.
    /// </summary>
    public sealed class NetworkFoodCatalogSource : IRemoteFoodCatalogSource
    {
        private const int PageLimit = 100;
        private const int MaxPages = 100;

        private readonly IEndPointManager _endPointManager;
        private readonly IYummyServiceV2Credentials _credentials;

        public NetworkFoodCatalogSource(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager ?? throw new ArgumentNullException(nameof(endPointManager));
            _credentials = endPointManager as IYummyServiceV2Credentials;
        }

        public async UniTask<FoodCatalogSourceResult> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetToken(out var token))
            {
                return FoodCatalogSourceResult.Empty("YummyService v2 の Unity Device token が未設定です。");
            }

            var baseUrl = _endPointManager.baseEndPointUrl;
            var items = new List<FoodCatalogItem>();
            var cursor = string.Empty;

            for (var page = 0; page < MaxPages; page++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!YummyServiceV2Url.TryBuildUnityDeviceOrdersUrl(
                        baseUrl,
                        "COMPLETED",
                        string.Empty,
                        string.Empty,
                        PageLimit,
                        cursor,
                        out var ordersUrl))
                {
                    return new FoodCatalogSourceResult(
                        items,
                        "API v2 のベースURLが未設定または不正です。");
                }

                using var request = UnityWebRequest.Get(ordersUrl);
                request.timeout = 15;
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {token}");

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
                    return new FoodCatalogSourceResult(items, DescribeRequestFailure(request));
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    return new FoodCatalogSourceResult(items, DescribeRequestFailure(request));
                }

                if (!TryParseResponse(
                        request.downloadHandler?.text ?? string.Empty,
                        out var response))
                {
                    return new FoodCatalogSourceResult(
                        items,
                        "API v2 の order history 応答JSONを解析できませんでした。");
                }

                if (response?.items == null)
                {
                    return new FoodCatalogSourceResult(
                        items,
                        "API v2 の order history 応答に items がありません。");
                }

                var pageItems = FoodCatalogTransportMapper.ToCatalogItems(response, baseUrl);
                if (response.items.Length > 0 && pageItems.Count == 0)
                {
                    return new FoodCatalogSourceResult(
                        items,
                        "API v2 の order history に利用可能な v2 status がありません。");
                }

                foreach (var item in pageItems) items.Add(item);

                if (!response.has_more) return new FoodCatalogSourceResult(items);

                if (string.IsNullOrWhiteSpace(response.next_cursor)
                    || string.Equals(cursor, response.next_cursor, StringComparison.Ordinal))
                {
                    return new FoodCatalogSourceResult(
                        items,
                        "API v2 の order history cursor が不正です。");
                }

                // The cursor is opaque.  Keep it byte-for-byte as returned by the
                // server and let the URL builder perform only URI escaping.
                cursor = response.next_cursor;
            }

            return new FoodCatalogSourceResult(
                items,
                "API v2 の order history がページ上限を超えました。");
        }

        private static bool TryParseResponse(string json, out DeviceOrderListResponseDto response)
        {
            response = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var root = JObject.Parse(json);
                // DeviceOrderListResponse marks all three fields required.  A
                // JsonUtility bool would silently become false when has_more is
                // missing, which could hide a truncated history, so check presence
                // before deserializing.
                if (!root.ContainsKey("items")
                    || !root.ContainsKey("next_cursor")
                    || !root.ContainsKey("has_more")) return false;

                response = root.ToObject<DeviceOrderListResponseDto>();
                return response != null;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private bool TryGetToken(out string token)
        {
            token = _credentials?.DeviceAccessToken;
            return !string.IsNullOrWhiteSpace(token);
        }

        private static string DescribeRequestFailure(UnityWebRequest request)
        {
            if (request.responseCode > 0)
            {
                return $"API v2 の Unity Device order history 取得に失敗しました ({(HttpStatusCode)request.responseCode})。";
            }

            return "API v2 の Unity Device order history に接続できませんでした。";
        }
    }
}
