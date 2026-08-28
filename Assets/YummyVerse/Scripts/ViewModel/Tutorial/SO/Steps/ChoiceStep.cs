using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Steps
{
    /// <summary>
    /// 分岐。選択結果を Blackboard に格納し、選択肢が持つサブシーケンスへ進む。
    /// 対応: S6 の初回判定, S16 のメニュー表示
    /// </summary>
    [CreateAssetMenu(fileName = "Step_", menuName = "YummyVerse/Tutorial/Step/Choice")]
    public class ChoiceStep : TutorialStep
    {
        [SerializeField] private LocalizedString prompt;
        [SerializeField] private List<ChoiceOption> options = new();
        [SerializeField, Min(0f)] private float timeoutSeconds = 15f;
        [SerializeField, Min(0)] private int defaultOptionIndex;

        [Tooltip("選択結果を格納する Blackboard のキー")]
        [SerializeField] private string blackboardKey = "choice";

        public override async UniTask ExecuteAsync(TutorialContext ctx, CancellationToken ct)
        {
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning($"[Tutorial] {StepId}: 選択肢が空のためスキップします");
                return;
            }

            var labels = new List<LocalizedString>(options.Count);
            foreach (var option in options) labels.Add(option?.Label);

            var fallbackIndex = Mathf.Clamp(defaultOptionIndex, 0, options.Count - 1);
            var index = await ctx.Choice.SelectAsync(prompt, labels, timeoutSeconds, fallbackIndex, ct);
            index = Mathf.Clamp(index, 0, options.Count - 1);

            await ctx.Choice.HideAsync(ct);

            var selected = options[index];
            if (selected == null) return;

            if (!string.IsNullOrEmpty(blackboardKey))
            {
                ctx.Blackboard[blackboardKey] = selected.Value;
            }

            switch (selected.FirstTimeUserEffect)
            {
                case FirstTimeUserEffect.SetTrue:
                    ctx.IsFirstTimeUser = true;
                    break;
                case FirstTimeUserEffect.SetFalse:
                    ctx.IsFirstTimeUser = false;
                    break;
            }

            Debug.Log($"[Tutorial] {StepId}: '{selected.Value}' が選択されました (index={index})");

            if (selected.SubSequence == null) return;

            if (ctx.RunSubSequenceAsync == null)
            {
                Debug.LogError($"[Tutorial] {StepId}: RunSubSequenceAsync が設定されていないためサブシーケンスを実行できません");
                return;
            }

            await ctx.RunSubSequenceAsync(selected.SubSequence, ct);
        }
    }
}
