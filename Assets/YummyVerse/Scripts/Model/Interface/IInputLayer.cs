using System;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IInputLayer
    {
        event Action OnConfigUIButtonClicked;

        event Action OnFoodDestroyButtonClicked;

        /// <summary>
        /// 決定/スタートボタン。チュートリアルの S1 や Narration の「ボタンで次へ」に使う。
        /// </summary>
        event Action OnStartButtonPressed;

        /// <summary>
        /// スタッフ用の強制リセット(既定 F5)。セッションを即座に中断して Attract へ戻す。
        /// </summary>
        event Action OnStaffResetPressed;
    }
}
