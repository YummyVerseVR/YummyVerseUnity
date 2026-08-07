using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Conditions
{
    /// <summary>指定イベントが n 回発火したら成立。</summary>
    [CreateAssetMenu(fileName = "Cond_Counted_", menuName = "YummyVerse/Tutorial/Condition/Counted Event")]
    public class CountedEventCondition : TutorialCondition
    {
        [SerializeField] private GameEventId eventId;
        [SerializeField, Min(1)] private int count = 3;

        public override async UniTask WaitAsync(TutorialContext ctx, CancellationToken ct)
        {
            // 回数のカウントはローカルのストリーム側に閉じるので、アセットを共有しても混線しない。
            await ctx.Events.GetStream(eventId).Take(count).LastAsync(ct);
        }
    }
}
