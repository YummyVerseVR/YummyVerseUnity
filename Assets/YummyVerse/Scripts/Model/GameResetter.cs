using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class GameResetter : IGameResetter
    {
        private readonly IQRDetectionService _qrDetectionService;
        private readonly IFoodContext _foodContext;
        private readonly IGameCommandBus _gameCommandBus;
        private readonly IIdleWatcher _idleWatcher;

        public GameResetter(
            IQRDetectionService qrDetectionService,
            IFoodContext foodContext,
            IGameCommandBus gameCommandBus,
            IIdleWatcher idleWatcher)
        {
            _qrDetectionService = qrDetectionService;
            _foodContext = foodContext;
            _gameCommandBus = gameCommandBus;
            _idleWatcher = idleWatcher;
        }

        /// <summary>
        /// 中断演出のキャンセルに巻き込まれてリセットが中途半端に終わることが無いよう、
        /// ここでは ct を待機の中断にのみ使い、状態の初期化自体は必ず全て実行する。
        /// </summary>
        public UniTask ResetAsync(CancellationToken ct)
        {
            Debug.Log("[Session] ゲーム状態をリセットします");

            // 皿の上の食品を消す(既存の FoodView.TryDestroyFood が働く)
            _gameCommandBus.Request(GameCommandId.HideMenu);
            _gameCommandBus.Request(GameCommandId.DestroyAllFood);

            // 注文内容・認識状態を捨てる
            _foodContext.Reset();
            _qrDetectionService.Reset();

            // 次の来場者を待つ間は無操作監視を止める
            _idleWatcher.SetActive(false);

            return UniTask.CompletedTask;
        }
    }
}
