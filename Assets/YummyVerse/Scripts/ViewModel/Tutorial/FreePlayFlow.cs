using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    /// <summary>
    /// FreePlay もチュートリアルと同じステップ列で表現する。
    /// 型を増やさずシーケンスを差し替えるだけで内容を変えられる。
    /// </summary>
    public class FreePlayFlow : IFreePlayFlow
    {
        private readonly ITutorialRunner _runner;
        private readonly TutorialConfig _config;
        private readonly IFoodSelectionFlow _foodSelectionFlow;

        public FreePlayFlow(
            ITutorialRunner runner,
            TutorialConfig config,
            IFoodSelectionFlow foodSelectionFlow)
        {
            _runner = runner;
            _config = config;
            _foodSelectionFlow = foodSelectionFlow;
        }

        public async UniTask RunAsync(TutorialContext ctx, CancellationToken ct)
        {
            // TutorialSequence_Main の完了直後に、API v2 + PersistentData の一覧から選択する。
            await _foodSelectionFlow.RunAsync(ct);

            if (_config.FreePlaySequence == null)
            {
                Debug.LogWarning("[FreePlay] freePlaySequence が未設定のためスキップします");
                return;
            }

            await _runner.RunAsync(_config.FreePlaySequence, ctx, ct);
        }
    }
}
