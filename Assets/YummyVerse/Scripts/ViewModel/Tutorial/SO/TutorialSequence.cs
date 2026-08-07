using System.Collections.Generic;
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO
{
    /// <summary>
    /// チュートリアルの進行順序。
    /// 分岐は ChoiceStep の結果でサブシーケンスを差し替える形にし、
    /// ステップ間に任意のジャンプは持たせない(デバッグ不能になるため)。
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialSequence_", menuName = "YummyVerse/Tutorial/Tutorial Sequence")]
    public class TutorialSequence : ScriptableObject
    {
        [SerializeField] private List<TutorialStep> steps = new();

        public IReadOnlyList<TutorialStep> Steps => steps;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (steps == null) return;

            var seen = new HashSet<string>();
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i] == null)
                {
                    Debug.LogWarning($"[Tutorial] {name}: {i} 番目のステップが未設定です", this);
                    continue;
                }

                if (!seen.Add(steps[i].StepId))
                {
                    Debug.LogWarning($"[Tutorial] {name}: stepId '{steps[i].StepId}' が重複しています", this);
                }
            }
        }
#endif
    }
}
