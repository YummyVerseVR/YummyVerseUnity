using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Localization;
using YummyVerse.Scripts.ViewModel.Interface;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    public class ChoicePresenter : IChoicePresenter, IDisposable
    {
        private const float FadeSeconds = 0.1f;

        private readonly Subject<int> _onSelected = new();

        public ReactiveProperty<string> Prompt { get; } = new(string.Empty);
        public ReactiveProperty<IReadOnlyList<string>> Options { get; } = new(Array.Empty<string>());
        public ReactiveProperty<bool> IsVisible { get; } = new(false);

        public void Select(int index)
        {
            if (!IsVisible.Value) return;
            _onSelected.OnNext(index);
        }

        public async UniTask<int> SelectAsync(
            LocalizedString prompt,
            IReadOnlyList<LocalizedString> options,
            float timeoutSeconds,
            int defaultIndex,
            CancellationToken ct)
        {
            Prompt.Value = await prompt.ResolveAsync(ct);

            var labels = new string[options?.Count ?? 0];
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i] = await options[i].ResolveAsync(ct);
            }

            Options.Value = labels;
            IsVisible.Value = true;

            var selectedIndex = -1;
            using var subscription = _onSelected.Subscribe(i =>
            {
                if (selectedIndex < 0) selectedIndex = i;
            });

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var winner = await UniTask.WhenAny(
                WaitForSelectionAsync(() => selectedIndex >= 0, linked.Token),
                WaitTimeoutAsync(timeoutSeconds, linked.Token));

            linked.Cancel();
            ct.ThrowIfCancellationRequested();

            if (winner == 0 && selectedIndex >= 0) return selectedIndex;

            Debug.Log($"[Tutorial] 選択がタイムアウトしたため既定値 index={defaultIndex} を採用します");
            return defaultIndex;
        }

        private static async UniTask WaitForSelectionAsync(Func<bool> predicate, CancellationToken ct)
        {
            await UniTask.WaitUntil(predicate, cancellationToken: ct).SuppressCancellationThrow();
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            IsVisible.Value = false;
            await UniTask.Delay(TimeSpan.FromSeconds(FadeSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct);
        }

        /// <summary>timeoutSeconds が 0 以下のときは無制限に待つ。</summary>
        private static async UniTask WaitTimeoutAsync(float timeoutSeconds, CancellationToken ct)
        {
            if (timeoutSeconds <= 0f)
            {
                await UniTask.Never(ct).SuppressCancellationThrow();
                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds), DelayType.UnscaledDeltaTime, cancellationToken: ct)
                .SuppressCancellationThrow();
        }

        public void Dispose()
        {
            _onSelected?.Dispose();
            Prompt?.Dispose();
            Options?.Dispose();
            IsVisible?.Dispose();
        }
    }
}
