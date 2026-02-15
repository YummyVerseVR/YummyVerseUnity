using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodDownloader
    {
        UniTask<FoodDownloadResult> Download(Guid guid, CancellationToken ct);
    }
}