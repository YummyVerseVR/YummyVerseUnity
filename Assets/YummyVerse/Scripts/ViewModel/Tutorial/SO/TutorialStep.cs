using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO
{
    /// <summary>
    /// 1ステップ = 提示(Present) → 完了条件の待機(Await) → 成功演出(Succeed)。
    ///
    /// 具象は Narration / Task / Choice の3種類のみ。これ以上増やさないこと。
    /// 「説明文だけ出す状態」は「完了条件が時間経過またはボタン入力である Narration」に過ぎない。
    /// </summary>
    public abstract class TutorialStep : ScriptableObject
    {
        [SerializeField] private string stepId;          // "S1", "S3.1" など。ログ・デバッグ用
        [SerializeField] private bool skippableOnRepeat; // 2周目以降スキップするか

        public string StepId => string.IsNullOrEmpty(stepId) ? name : stepId;
        public bool SkippableOnRepeat => skippableOnRepeat;

        public abstract UniTask ExecuteAsync(TutorialContext ctx, CancellationToken ct);
    }
}
