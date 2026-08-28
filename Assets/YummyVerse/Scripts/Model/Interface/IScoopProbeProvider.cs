using System.Collections.Generic;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// いま食べ物へ触れうる「手」のワールド座標を供給する境界。
    /// コントローラー使用時はコントローラー、ハンドトラッキング時は手の位置を返す。
    /// 当たり判定側は左右や入力方式を意識しない。
    /// </summary>
    public interface IScoopProbeProvider
    {
        /// <summary>
        /// 追跡できている手だけを返す。戻り値は呼び出しごとに再利用されるため、保持しないこと。
        /// </summary>
        IReadOnlyList<ScoopProbe> GetProbes(float radius);
    }
}
