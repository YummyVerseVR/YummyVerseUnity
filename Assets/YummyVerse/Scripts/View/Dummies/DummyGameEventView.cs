using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Dummies
{
    /// <summary>
    /// すくい判定・完食判定の実装がまだ無いため、チュートリアルを通しで動かすための手動発火。
    /// ゲーム側の本実装が入ったらこの GameObject を外すだけでよい。
    ///
    /// 既存の DummyQRView と同じく、Inspector のコンテキストメニューかキー入力から叩く。
    /// </summary>
    public class DummyGameEventView : MonoBehaviour
    {
        [Header("キーボードショートカット (エディタ確認用)")]
        [SerializeField] private KeyCode scoopKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode dishClearedKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode userAbsentKey = KeyCode.Alpha3;

        [Tooltip("Bボタンでの食べ物破棄を暫定的に「完食」として扱う。本実装が入ったら外すこと。")]
        [SerializeField] private bool treatFoodDestroyAsDishCleared = true;

        private IGameEventPublisher _publisher;
        private IFoodViewModel _foodViewModel;

        [Inject]
        public void Construct(IGameEventPublisher publisher, IFoodViewModel foodViewModel)
        {
            _publisher = publisher;
            _foodViewModel = foodViewModel;
        }

        private void Start()
        {
            if (!treatFoodDestroyAsDishCleared) return;

            Observable.FromEvent(
                    h => _foodViewModel.OnFoodDestroy += h,
                    h => _foodViewModel.OnFoodDestroy -= h)
                .Subscribe(_ => FakeDishCleared()).AddTo(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(scoopKey)) FakeFoodScooped();
            if (Input.GetKeyDown(dishClearedKey)) FakeDishCleared();
            if (Input.GetKeyDown(userAbsentKey)) FakeUserAbsent();
        }

        [ContextMenu("Fake: Food Scooped")]
        public void FakeFoodScooped() => _publisher?.PublishFoodScooped();

        [ContextMenu("Fake: Dish Cleared")]
        public void FakeDishCleared() => _publisher?.PublishDishCleared();

        [ContextMenu("Fake: User Absent")]
        public void FakeUserAbsent() => _publisher?.PublishUserAbsent();
    }
}
