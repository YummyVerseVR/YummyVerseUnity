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

        public NetworkConnectionTester(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager;
        }

        public async UniTask<TestConnectionResult> TestConnection(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!YummyServiceV2Url.TryBuildMenuUrl(_endPointManager.baseEndPointUrl, out var menuUrl))
            {
                return new TestConnectionResult
                {
                    success = false,
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            using var request = UnityWebRequest.Get(menuUrl);
            request.timeout = 10;
            request.SetRequestHeader("Accept", "application/json");
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
