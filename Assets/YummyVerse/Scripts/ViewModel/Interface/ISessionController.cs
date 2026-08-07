namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// 来場者1人ぶんのセッションを回す。
    /// 来場者は途中で必ず離脱するので、中断は CancellationToken の一括伝播で処理する。
    /// ステップごとに Attract への戻り線は書かない。
    /// </summary>
    public interface ISessionController
    {
        /// <summary>セッションを中断して Attract へ戻す。</summary>
        void AbortSession();
    }
}
