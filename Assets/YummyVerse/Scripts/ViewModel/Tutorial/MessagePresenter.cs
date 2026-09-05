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

        /// <summary>本文と補助行を別々に持ち、View へは組み立てた1つの文字列として渡す。</summary>
        private string _body = string.Empty;
        private string _subText;

        public UniTask ShowAsync(LocalizedString msg, CancellationToken ct) => ShowAsync(msg, null, ct);

        public async UniTask ShowAsync(LocalizedString msg, string subText, CancellationToken ct)
        {
            _body = await msg.ResolveAsync(ct);
            _subText = subText;
            ApplyText();

            IsVisible.Value = true;
            await UniTask.Delay(TimeSpan.FromSeconds(FadeSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        public void SetSubText(string subText)
        {
            _subText = subText;
            ApplyText();
        }

        private void ApplyText() =>
            Text.Value = string.IsNullOrEmpty(_subText) ? _body : _body + "\n" + _subText;

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
