using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;

namespace YummyVerse.Scripts.Infrastructure
{
    public class NetworkConnectionTester : INetworkConnectionTester
    {
        private readonly IEndPointManager _endPointManager;
        private readonly IYummyServiceV2Credentials _credentials;

        public NetworkConnectionTester(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager ?? throw new System.ArgumentNullException(nameof(endPointManager));
            _credentials = endPointManager as IYummyServiceV2Credentials;
        }

        public async UniTask<TestConnectionResult> TestConnection(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!YummyServiceV2Url.TryBuildUnityDeviceOrdersUrl(
                    _endPointManager.baseEndPointUrl,
                    "COMPLETED",
                    string.Empty,
                    string.Empty,
                    1,
                    string.Empty,
                    out var ordersUrl))
            {
                return new TestConnectionResult
                {
                    success = false,
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            if (string.IsNullOrWhiteSpace(_credentials?.DeviceAccessToken))
            {
                return new TestConnectionResult
                {
                    success = false,
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            using var request = UnityWebRequest.Get(ordersUrl);
            request.timeout = 10;
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_credentials.DeviceAccessToken}");

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
                // responseCode=0 は DNS/TLS/transport failure。
            }

            return new TestConnectionResult
            {
                success = request.result == UnityWebRequest.Result.Success,
                StatusCode = request.responseCode > 0 ? (HttpStatusCode)request.responseCode : 0
            };
        }
    }
}
