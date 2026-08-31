using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Conditions
{
    /// <summary>指定した GameEventId が1回発火したら成立。</summary>
    [CreateAssetMenu(fileName = "Cond_Event_", menuName = "YummyVerse/Tutorial/Condition/Game Event")]
    public class GameEventCondition : TutorialCondition
    {
        [SerializeField] private GameEventId eventId;

        public override async UniTask WaitAsync(TutorialContext ctx, CancellationToken ct)
        {
            await ctx.Events.GetStream(eventId).FirstAsync(ct);
        }
    }
}
