using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// ナレーション音声の再生。NarrationStep の voiceClip の受け皿。
    /// </summary>
    public interface IVoicePresenter
    {
        Observable<AudioClip> OnPlay { get; }
        Observable<Unit> OnStop { get; }

        /// <summary>クリップの長さぶん待機する。null のときは即座に完了する。</summary>
        UniTask PlayAsync(AudioClip clip, CancellationToken ct);

        void Stop();
    }
}
