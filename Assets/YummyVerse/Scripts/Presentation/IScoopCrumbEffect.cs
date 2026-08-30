using UnityEngine;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// すくいが成立した位置に食べかすを出す表示側の口。
    /// 当たり判定の View はこの契約だけを知り、Particle の作り方には依存しない。
    /// </summary>
    public interface IScoopCrumbEffect
    {
        /// <param name="position">噴き出す位置 (world)</param>
        /// <param name="direction">飛び散る中心方向 (world)</param>
        void Play(Vector3 position, Vector3 direction);
    }
}
