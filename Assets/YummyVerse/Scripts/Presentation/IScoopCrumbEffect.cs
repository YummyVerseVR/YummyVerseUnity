using UnityEngine;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// すくいが成立した位置に食べかすを出す表示側の口。
    /// 当たり判定の View はこの契約だけを知り、Particle の作り方には依存しない。
    /// </summary>
    public interface IScoopCrumbEffect
    {
        /// <param name="position">
        /// インタラクションが起きた位置 (world)。ここから上へ弾ける。
        /// </param>
        void Play(Vector3 position);
    }
}
