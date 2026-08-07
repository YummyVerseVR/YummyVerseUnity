using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO
{
    /// <summary>
    /// ステップの完了条件。
    ///
    /// 【重要】ScriptableObject は複数のステップから共有されるアセットなので、
    /// 待機中の進捗などの状態をフィールドに持たせてはならない。すべてローカル変数で扱うこと。
    /// </summary>
    public abstract class TutorialCondition : ScriptableObject
    {
        /// <summary>条件成立まで待機する。キャンセル時は OperationCanceledException。</summary>
        public abstract UniTask WaitAsync(TutorialContext ctx, CancellationToken ct);
    }
}
