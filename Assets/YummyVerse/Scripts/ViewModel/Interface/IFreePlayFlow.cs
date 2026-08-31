using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.ViewModel.Tutorial;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// チュートリアル完走後の自由体験(S15〜S19)。
    /// シーン遷移もロードも暗転も挟まず、チュートリアルからそのまま続く。
    /// </summary>
    public interface IFreePlayFlow
    {
        UniTask RunAsync(TutorialContext ctx, CancellationToken ct);
    }
}
