using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    /// <summary>
    /// シーケンスのステップを順に実行するだけの層。
    /// ゲームのコンポーネントは一切参照しない。
    /// </summary>
    public class TutorialRunner : ITutorialRunner, IDisposable
    {
        /// <summary>サブシーケンスの入れ子がこの深さを超えたら設定ミスとみなす。</summary>
        private const int MaxSubSequenceDepth = 4;

        private readonly ReactiveProperty<string> _currentStepId = new(string.Empty);
        public ReadOnlyReactiveProperty<string> CurrentStepId => _currentStepId;

        private float _stepStartedAt;
        private int _depth;

        public float CurrentStepElapsedSeconds =>
            string.IsNullOrEmpty(_currentStepId.Value) ? 0f : Time.realtimeSinceStartup - _stepStartedAt;

        public async UniTask RunAsync(TutorialSequence sequence, TutorialContext ctx, CancellationToken ct)
        {
            // ChoiceStep からサブシーケンスを実行できるようにしておく。
            // 任意のステップへのジャンプは提供しない。
            ctx.RunSubSequenceAsync ??= (sub, token) => RunAsync(sub, ctx, token);

            try
            {
                await RunInternalAsync(sequence, ctx, ct);
            }
            finally
            {
                if (_depth == 0) _currentStepId.Value = string.Empty;
            }
        }

        private async UniTask RunInternalAsync(TutorialSequence sequence, TutorialContext ctx, CancellationToken ct)
        {
            if (sequence == null)
            {
                Debug.LogWarning("[Tutorial] シーケンスが未設定のため何も実行しません");
                return;
            }

            if (_depth >= MaxSubSequenceDepth)
            {
                Debug.LogError($"[Tutorial] サブシーケンスの入れ子が深すぎます ({sequence.name})。設定を見直してください。");
                return;
            }

            _depth++;
            try
            {
                foreach (var step in sequence.Steps)
                {
                    ct.ThrowIfCancellationRequested();

                    if (step == null) continue;
                    if (!ctx.IsFirstTimeUser && step.SkippableOnRepeat)
                    {
                        Debug.Log($"[Tutorial] Skip  {step.StepId} (2周目以降)");
                        continue;
                    }

                    using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                    _currentStepId.Value = step.StepId;
                    _stepStartedAt = Time.realtimeSinceStartup;
                    Debug.Log($"[Tutorial] Enter {step.StepId}");

                    // 例外は握り潰さず、上位のセッション管理へ伝播させる
                    await step.ExecuteAsync(ctx, stepCts.Token);

                    Debug.Log($"[Tutorial] Exit  {step.StepId} ({Time.realtimeSinceStartup - _stepStartedAt:F1}s)");
                }
            }
            finally
            {
                _depth--;
            }
        }

        public void Dispose()
        {
            _currentStepId?.Dispose();
        }
    }
}
