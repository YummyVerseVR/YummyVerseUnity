using System;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// 設定ダイアログから見た仮想キーボード。
    /// </summary>
    /// <remarks>
    /// View 層の具体的な実装 (VirtualKeyboardView) に Presentation が依存しないための口。
    /// 必要なのは「まだ打鍵の途中か」「閉じろ」「閉じたときの中身」の3つだけ。
    /// </remarks>
    public interface IVirtualKeyboard
    {
        /// <summary>キーボードが開いている = まだ打鍵の途中かどうか。</summary>
        bool IsEditing { get; }

        /// <summary>閉じたとき、そのときの入力欄の中身を渡す。編集の確定はここ一本。</summary>
        event Action<string> EditingFinished;

        /// <summary>キーボードを閉じる。開いていなければ何も起きない。</summary>
        void Close();
    }
}
