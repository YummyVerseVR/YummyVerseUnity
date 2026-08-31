using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// ゲーム機能側がイベントを発火するための口(発火側)。
    /// チュートリアルはこの interface に依存しない。
    ///
    /// 既存実装から自動的に橋渡しされるイベント(QR検出/ロスト、メニュー選択、スタートボタン)は
    /// GameEventBus 内部で配線済みなので、ここを呼ぶ必要はない。
    /// 現時点で発火元の実装が存在しない FoodScooped / DishCleared のために公開している。
    /// </summary>
    public interface IGameEventPublisher
    {
        void PublishFoodScooped();
        void PublishDishCleared();
        void PublishMenuItemSelected(MenuItem item);
        void PublishUserAbsent();

        /// <summary>セッションをまたいで持ち越してはいけない状態(直近の選択など)を捨てる。</summary>
        void ResetSessionState();
    }
}
