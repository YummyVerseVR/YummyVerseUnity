using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodContext
    {
        ReactiveProperty<FoodDownloadResult> downloadResult { get; }

        /// <summary>
        /// 食べ物を用意している間 true。選択画面へ入った時点から、
        /// ダウンロードが成否どちらかで決着するまで立ち続ける。
        /// 表示側はこの間、食べ物が出る位置にフードドームを被せて「準備中」を伝える。
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsPreparing { get; }

        /// <summary>
        /// 選択画面へ入ったことを伝え、準備中の表示を始める。
        /// カタログの読込中も選択待ちも「まだ食べ物が出せない」点では同じなので、
        /// メニューを出す時点からここを立てる。
        /// </summary>
        void BeginPreparation();

        /// <summary>
        /// セッション終了時にダウンロード結果を初期化する。
        /// </summary>
        void Reset();
    }
}
