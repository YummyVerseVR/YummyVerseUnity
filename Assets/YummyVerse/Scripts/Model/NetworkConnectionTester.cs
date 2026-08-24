using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class NetworkConnectionTester : INetworkConnectionTester
    {
        public UniTask<TestConnectionResult> TestConnection(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // 現行 v2 OpenAPI には path/auth/compatibility operation が存在しない。
            // 任意 URL や旧 server へ probing request を送らず、契約未公開として fail closed にする。
            return UniTask.FromResult(new TestConnectionResult
            {
                success = false,
                StatusCode = HttpStatusCode.ServiceUnavailable
            });
        }
    }
}
