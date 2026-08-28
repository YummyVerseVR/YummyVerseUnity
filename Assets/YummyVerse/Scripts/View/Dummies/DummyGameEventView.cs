using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Dummies
{
    /// <summary>
    /// XR デバイスが無いエディタでチュートリアルを通しで確認するための手動入力。
    /// 本番フローには不要なので、この GameObject をシーンへ置かなければよい。
    ///
    /// すくい/完食は実装済みの IFoodEatingService を叩くだけにしてある。
    /// イベントを直接偽装すると残量や当たり判定と食い違うため、ここでは行わない。
    /// </summary>
    public class DummyGameEventView : MonoBehaviour
    {
        [Header("キーボードショートカット (エディタ確認用)")]
        [SerializeField] private KeyCode scoopKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode dishClearedKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode userAbsentKey = KeyCode.Alpha3;

        private IGameEventPublisher _publisher;
        private IFoodEatingService _foodEatingService;

        [Inject]
        public void Construct(IGameEventPublisher publisher, IFoodEatingService foodEatingService)
        {
            _publisher = publisher;
            _foodEatingService = foodEatingService;
        }

        private void Update()
        {
            if (Input.GetKeyDown(scoopKey)) SimulateFoodScooped();
            if (Input.GetKeyDown(dishClearedKey)) SimulateDishCleared();
            if (Input.GetKeyDown(userAbsentKey)) FakeUserAbsent();
        }

        [ContextMenu("Simulate: Food Scooped")]
        public void SimulateFoodScooped()
        {
            if (_foodEatingService != null && _foodEatingService.TryScoop()) return;
            Debug.LogWarning("[Eating] すくえる食べ物が皿の上にありません。");
        }

        [ContextMenu("Simulate: Dish Cleared")]
        public void SimulateDishCleared()
        {
            if (_foodEatingService != null && _foodEatingService.ForceClear()) return;
            Debug.LogWarning("[Eating] 完食できる食べ物が皿の上にありません。");
        }

        [ContextMenu("Fake: User Absent")]
        public void FakeUserAbsent() => _publisher?.PublishUserAbsent();
    }
}
