using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Conditions
{
    /// <summary>
    /// 子条件のいずれかが成立したら成立。
    /// これがあるので「ボタン押下 または 5秒経過で進む」に新しいクラスは要らない。
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_AnyOf_", menuName = "YummyVerse/Tutorial/Condition/Any Of")]
    public class AnyOfCondition : TutorialCondition
    {
        [SerializeField] private List<TutorialCondition> conditions = new();

        public override async UniTask WaitAsync(TutorialContext ctx, CancellationToken ct)
        {
            if (conditions == null || conditions.Count == 0)
            {
                Debug.LogWarning($"[Tutorial] {name}: 子条件が空のため即座に成立扱いにします");
                return;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var tasks = new List<UniTask>(conditions.Count);
            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                tasks.Add(WaitSuppressedAsync(condition, ctx, linked.Token));
            }

            if (tasks.Count == 0) return;

            await UniTask.WhenAny(tasks);
            linked.Cancel();

            // 本物のセッション中断だけを上位へ伝播させる
            ct.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// 敗者は linked.Cancel() で畳まれるため、その OperationCanceledException は握り潰す。
        /// </summary>
        private static async UniTask WaitSuppressedAsync(TutorialCondition condition, TutorialContext ctx, CancellationToken ct)
        {
            await condition.WaitAsync(ctx, ct).SuppressCancellationThrow();
        }
    }
}
