using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodContext
    {
        ReactiveProperty<FoodDownloadResult> downloadResult { get; }

        /// <summary>
        /// セッション終了時にダウンロード結果を初期化する。
        /// </summary>
        void Reset();
    }
}