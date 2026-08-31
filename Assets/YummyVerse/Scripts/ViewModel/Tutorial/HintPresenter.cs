using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.Video;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    public class HintPresenter : IHintPresenter, IDisposable
    {
        private const float FadeSeconds = 0.1f;

        public ReactiveProperty<string> Text { get; } = new(string.Empty);
        public ReactiveProperty<VideoClip> DemoClip { get; } = new();
        public ReactiveProperty<bool> IsVisible { get; } = new(false);

        public async UniTask ShowAsync(HintPresentation hint, CancellationToken ct)
        {
            if (hint == null)
            {
                await HideAsync(ct);
                return;
            }

            Text.Value = hint.ShowsText ? await hint.Text.ResolveAsync(ct) : string.Empty;
            DemoClip.Value = hint.DemoClip;
            IsVisible.Value = true;
            await UniTask.Delay(TimeSpan.FromSeconds(FadeSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            IsVisible.Value = false;
            DemoClip.Value = null;
            await UniTask.Delay(TimeSpan.FromSeconds(FadeSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        public void Dispose()
        {
            Text?.Dispose();
            DemoClip?.Dispose();
            IsVisible?.Dispose();
        }
    }
}
