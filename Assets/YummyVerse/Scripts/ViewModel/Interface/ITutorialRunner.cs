using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using YummyVerse.Scripts.ViewModel.Tutorial;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface ITutorialRunner
    {
        /// <summary>実行中のステップID。デバッグHUD用。</summary>
        ReadOnlyReactiveProperty<string> CurrentStepId { get; }

        /// <summary>現在のステップに入ってからの経過秒数。</summary>
        float CurrentStepElapsedSeconds { get; }

        UniTask RunAsync(TutorialSequence sequence, TutorialContext ctx, CancellationToken ct);
    }
}
