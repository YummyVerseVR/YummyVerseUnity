using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Steps
{
    /// <summary>
    /// 提示して、条件成立で次へ。成功演出なし。
    /// 対応: S1, S2, S5, S6', S14, S15, S18, S19
    /// </summary>
    [CreateAssetMenu(fileName = "Step_", menuName = "YummyVerse/Tutorial/Step/Narration")]
    public class NarrationStep : TutorialStep
    {
        [SerializeField] private LocalizedString message;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private TutorialCondition completionCondition;

        [Header("完了時にゲーム側へ依頼するコマンド (S7 のような「ゲームに何かさせる」処理)")]
        [SerializeField] private GameCommandId onCompletedCommand = GameCommandId.None;

        [Header("メッセージを出したままにするか (次のステップへ表示を引き継ぐとき)")]
        [SerializeField] private bool keepMessageVisible;

        public override async UniTask ExecuteAsync(TutorialContext ctx, CancellationToken ct)
        {
            await ctx.Message.ShowAsync(message, ct);

            // 音声はメッセージ表示と並走させる。完了条件の待機を音声で縛らない。
            ctx.Voice.PlayAsync(voiceClip, ct).SuppressCancellationThrow().Forget();

            if (completionCondition == null)
            {
                Debug.LogWarning($"[Tutorial] {StepId}: completionCondition が未設定のため即座に次へ進みます");
            }
            else
            {
                await completionCondition.WaitAsync(ctx, ct);
            }

            ctx.Voice.Stop();

            if (!keepMessageVisible)
            {
                await ctx.Message.HideAsync(ct);
            }

            ctx.Commands.Request(onCompletedCommand);
        }
    }
}
