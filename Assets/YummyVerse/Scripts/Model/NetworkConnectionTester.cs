using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    public class NetworkConnectionTester : INetworkConnectionTester
    {
        private readonly IEndPointManager _endPointManager;
        
        private CompositeDisposable _disposables = new ();

        public NetworkConnectionTester(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager;
        }

        public async UniTask<TestConnectionResult> TestConnection(CancellationToken ct)
        {
            var url = _endPointManager.baseEndPointUrl;
            var result = new TestConnectionResult();
            using UnityWebRequest req = UnityWebRequest.Get(url);
            req.timeout = 10;
            var res = await req.SendWebRequest().WithCancellation(ct);
            result.success = res.result != UnityWebRequest.Result.ConnectionError;
            result.StatusCode = (HttpStatusCode)res.responseCode;
            return result;
        }
    }
}