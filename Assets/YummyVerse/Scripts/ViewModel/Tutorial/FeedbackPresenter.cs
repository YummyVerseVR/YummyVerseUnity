using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    public class FeedbackPresenter : IFeedbackPresenter, IDisposable
    {
        private const float FadeSeconds = 0.1f;

        private readonly Subject<AudioClip> _onPlaySfx = new();

        public ReactiveProperty<string> Label { get; } = new(string.Empty);
        public ReactiveProperty<bool> IsVisible { get; } = new(false);
        public Observable<AudioClip> OnPlaySfx => _onPlaySfx;

        public async UniTask PlaySuccessAsync(SuccessFeedbackAsset asset, CancellationToken ct)
        {
            if (asset == null)
            {
                // 演出アセット未設定でも進行は止めない
                return;
            }

            Label.Value = await asset.Label.ResolveAsync(ct);
            IsVisible.Value = true;
            if (asset.Sfx != null) _onPlaySfx.OnNext(asset.Sfx);

            await UniTask.Delay(TimeSpan.FromSeconds(asset.DurationSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);

            await HideAsync(ct);
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            IsVisible.Value = false;
            await UniTask.Delay(TimeSpan.FromSeconds(FadeSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        public void Dispose()
        {
            _onPlaySfx?.Dispose();
            Label?.Dispose();
            IsVisible?.Dispose();
        }
    }
}
