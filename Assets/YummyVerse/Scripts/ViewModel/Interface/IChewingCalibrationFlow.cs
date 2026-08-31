using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.ViewModel.Tutorial;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// 「ボタンを押してスタート」の直後、「YummyVerse へようこそ」の手前に挟まる咀嚼計の較正。
    ///
    /// チュートリアル本体のステップ列 (Narration / Task / Choice) には載せない。
    /// 較正はハードウェアの応答で進む処理であり、時間やボタンで進む提示とは性質が違うため。
    /// </summary>
    public interface IChewingCalibrationFlow
    {
        UniTask RunAsync(TutorialContext ctx, CancellationToken ct);
    }
}
