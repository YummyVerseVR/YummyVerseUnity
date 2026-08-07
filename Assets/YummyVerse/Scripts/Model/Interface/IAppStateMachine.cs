using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// アプリ全体の粗いモード管理。
    /// 遷移は Attract → Tutorial → FreePlay → Outro → Attract の一方向 +
    /// 任意の状態からの Attract への強制復帰のみ。
    /// </summary>
    public interface IAppStateMachine
    {
        ReadOnlyReactiveProperty<AppState> Current { get; }

        /// <summary>
        /// 状態を遷移させる。不正な遷移は行わず false を返す。
        /// </summary>
        bool TrySet(AppState next);
    }
}
