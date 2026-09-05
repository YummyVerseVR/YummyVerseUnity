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

        /// <summary>
        /// この基準がアプリの再起動をまたいで同じ物理位置を指すか。
        /// false のときに保存済みの配置を復元すると、現実と無関係な場所に食品が出る。
        /// </summary>
        bool SurvivesRestart { get; }

        /// <summary>
        /// 基準の世代を表す識別子。ランタイムが空間を作り直すと変わる。
        /// 保存時の値と一致しない配置は、同じ物理位置を指さないので使ってはいけない。
        /// 世代を持たない基準では空文字。
        /// </summary>
        string GenerationId { get; }
    }
}
