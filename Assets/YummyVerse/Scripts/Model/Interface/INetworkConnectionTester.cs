using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface INetworkConnectionTester
    {
        UniTask<TestConnectionResult> TestConnection(CancellationToken ct);
    }
}