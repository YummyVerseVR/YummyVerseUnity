using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.Localization;
using YummyVerse.Scripts.ViewModel.Interface;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    public class MessagePresenter : IMessagePresenter, IDisposable
    {
        /// <summary>View 側のフェード時間(ConfigUIView と揃えている)。</summary>
        private const float FadeSeconds = 0.1f;

        public ReactiveProperty<string> Text { get; } = new(string.Empty);
        public ReactiveProperty<bool> IsVisible { get; } = new(false);

        public async UniTask ShowAsync(LocalizedString msg, CancellationToken ct)
        {
            Text.Value = await msg.ResolveAsync(ct);
            IsVisible.Value = true;
            await UniTask.Delay(TimeSpan.FromSeconds(FadeSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            IsVisible.Value = false;
            await UniTask.Delay(TimeSpan.FromSeconds(FadeSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        public void Dispose()
        {
            Text?.Dispose();
            IsVisible?.Dispose();
        }
    }
}
