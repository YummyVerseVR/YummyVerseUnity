using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    public class VoicePresenter : IVoicePresenter, IDisposable
    {
        private readonly Subject<AudioClip> _onPlay = new();
        private readonly Subject<Unit> _onStop = new();

        public Observable<AudioClip> OnPlay => _onPlay;
        public Observable<Unit> OnStop => _onStop;

        public async UniTask PlayAsync(AudioClip clip, CancellationToken ct)
        {
            if (clip == null) return;

            _onPlay.OnNext(clip);
            await UniTask.Delay(TimeSpan.FromSeconds(clip.length), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        public void Stop()
        {
            _onStop.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _onPlay?.Dispose();
            _onStop?.Dispose();
        }
    }
}
