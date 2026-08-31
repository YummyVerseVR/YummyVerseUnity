using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Conditions
{
    /// <summary>指定秒数の経過で成立。</summary>
    [CreateAssetMenu(fileName = "Cond_Time_", menuName = "YummyVerse/Tutorial/Condition/Time Elapsed")]
    public class TimeElapsedCondition : TutorialCondition
    {
        [SerializeField, Min(0f)] private float seconds = 3f;

        public override async UniTask WaitAsync(TutorialContext ctx, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }
    }
}
