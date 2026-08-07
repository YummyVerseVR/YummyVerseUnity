using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO.Conditions
{
    /// <summary>決定ボタンの押下で成立。</summary>
    [CreateAssetMenu(fileName = "Cond_ButtonPressed", menuName = "YummyVerse/Tutorial/Condition/Button Pressed")]
    public class ButtonPressedCondition : TutorialCondition
    {
        public override async UniTask WaitAsync(TutorialContext ctx, CancellationToken ct)
        {
            await ctx.Events.GetStream(GameEventId.StartButtonPressed).FirstAsync(ct);
        }
    }
}
