using R3;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// 物理空間に固定された基準 Transform を供給する。
    ///
    /// 食品の置き場所は必ずこの Transform の子として持つ。ワールド座標で持ってはいけない。
    /// Unity のワールド原点は HMD の着脱で張り直されうるため、ワールド座標は
    /// 「部屋のどこか」を表さない。
    /// </summary>
    public interface IPlacementReferenceFrame
    {
        /// <summary>基準 Transform。まだ解決できていない間は null。</summary>
        Transform Current { get; }

        /// <summary>基準が解決できているか。false の間は食品の置き場所を決められない。</summary>
        ReadOnlyReactiveProperty<bool> IsReady { get; }

        /// <summary>
        /// 基準の種類。保存データがどの基準で測られたかを記録し、
        /// 別の基準で測った値を取り違えないために使う。
        /// </summary>
        string Kind { get; }
    }
}
