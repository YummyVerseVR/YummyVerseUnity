using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO.Tutorial;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Steps
{
    /// <summary>
    /// 指示を出し、ゲームイベントの達成を待つ。
    /// 滞留したらヒントを強化し、それでも進まなければ救済する。
    /// 仕様書の「指示」と「催促」は必ず1ステップに統合する(S3+S3.1, S8+S9, S11+S12)。
    /// </summary>
    [CreateAssetMenu(fileName = "Step_", menuName = "YummyVerse/Tutorial/Step/Task")]
    public class TaskStep : TutorialStep
    {
        [SerializeField] private LocalizedString instruction;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private TutorialCondition successCondition;

        [Header("指示を出した時点でゲーム側へ依頼するコマンド (S7 のような「ゲームに何かさせる」処理)")]
        [SerializeField] private GameCommandId onStartedCommand = GameCommandId.None;

        [Header("滞留時のヒント")]
        [SerializeField] private HintPresentation hint;
        [SerializeField, Min(0f)] private float hintDelaySeconds = 5f;

        [Header("救済 (来場者を絶対に立ち往生させないこと)")]
        [SerializeField, Min(1f)] private float rescueTimeoutSeconds = 30f;
        [SerializeField] private RescuePolicy rescuePolicy = RescuePolicy.AutoAdvance;
        [Tooltip("rescuePolicy が ForceComplete のときにゲーム側へ送るコマンド")]
        [SerializeField] private GameCommandId forceCompleteCommand = GameCommandId.DestroyAllFood;

        [Header("成功演出")]
        [SerializeField] private SuccessFeedbackAsset successFeedback;

        public override async UniTask ExecuteAsync(TutorialContext ctx, CancellationToken ct)
        {
            var startedAt = Time.realtimeSinceStartup;
            ctx.Analytics.Record(StepId, TutorialStepPhase.Entered, 0f);

            await ctx.Message.ShowAsync(instruction, ct);
            ctx.Voice.PlayAsync(voiceClip, ct).SuppressCancellationThrow().Forget();

            // 指示が出てから依頼する。S8 の前菜のように「指示と同時に対象を出す」ものはここで揃う。
            ctx.Commands.Request(onStartedCommand);

            if (successCondition == null)
            {
                Debug.LogWarning($"[Tutorial] {StepId}: successCondition が未設定のため達成を待てません。救済扱いにします。");
            }

            // ヒント / 成功待ち / 救済タイムアウト を並列で走らせ、決着したら残りを畳む。
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);

            ShowHintAfterDelayAsync(ctx, startedAt, linked.Token).Forget();

            var winner = await UniTask.WhenAny(
                WaitForSuccessAsync(ctx, linked.Token),
                WaitForRescueAsync(linked.Token));

            linked.Cancel();

            // 敗者のキャンセルではなく本物のセッション中断だったときだけ上位へ伝播させる
            ct.ThrowIfCancellationRequested();

            var elapsed = Time.realtimeSinceStartup - startedAt;
            var succeeded = winner == 0 && successCondition != null;

            if (succeeded)
            {
                ctx.Analytics.Record(StepId, TutorialStepPhase.Succeeded, elapsed);
                await ctx.Hint.HideAsync(ct);
                await ctx.Message.HideAsync(ct);
                await ctx.Feedback.PlaySuccessAsync(successFeedback, ct);
            }
            else
            {
                ctx.Analytics.Record(StepId, TutorialStepPhase.Rescued, elapsed);
                Debug.LogWarning($"[Tutorial] {StepId}: {rescueTimeoutSeconds}秒で達成されなかったため {rescuePolicy} で救済します");
                await ctx.Hint.HideAsync(ct);
                await ctx.Message.HideAsync(ct);
                ApplyRescue(ctx);
            }

            ctx.Voice.Stop();
        }

        private void ApplyRescue(TutorialContext ctx)
        {
            switch (rescuePolicy)
            {
                case RescuePolicy.AutoAdvance:
                    // 達成扱いにせず、そのまま次のステップへ進む
                    break;
                case RescuePolicy.ForceComplete:
                    ctx.Commands.Request(forceCompleteCommand);
                    break;
                case RescuePolicy.ReturnToAttract:
                    ctx.RequestAbort();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rescuePolicy), rescuePolicy, null);
            }
        }

        private async UniTask WaitForSuccessAsync(TutorialContext ctx, CancellationToken ct)
        {
            if (successCondition == null)
            {
                // 成功しえないので、救済タイムアウトに必ず負けるよう永久に待つ
                await UniTask.Never(ct).SuppressCancellationThrow();
                return;
            }

            await successCondition.WaitAsync(ctx, ct).SuppressCancellationThrow();
        }

        private async UniTask WaitForRescueAsync(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(rescueTimeoutSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct)
                .SuppressCancellationThrow();
        }

        private async UniTaskVoid ShowHintAfterDelayAsync(TutorialContext ctx, float startedAt, CancellationToken ct)
        {
            if (hint == null) return;

            var canceled = await UniTask
                .Delay(TimeSpan.FromSeconds(hintDelaySeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct)
                .SuppressCancellationThrow();
            if (canceled) return;

            ctx.Analytics.Record(StepId, TutorialStepPhase.HintShown, Time.realtimeSinceStartup - startedAt);
            await ctx.Hint.ShowAsync(hint, ct).SuppressCancellationThrow();
        }
    }
}
