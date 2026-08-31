using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.Localization;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// 選択肢の提示と選択待ち。タイムアウト時は既定値を返す。
    /// </summary>
    public interface IChoicePresenter
    {
        ReactiveProperty<string> Prompt { get; }
        ReactiveProperty<IReadOnlyList<string>> Options { get; }
        ReactiveProperty<bool> IsVisible { get; }

        /// <summary>View 側のボタンから呼ぶ。</summary>
        void Select(int index);

        UniTask<int> SelectAsync(
            LocalizedString prompt,
            IReadOnlyList<LocalizedString> options,
            float timeoutSeconds,
            int defaultIndex,
            CancellationToken ct);

        UniTask HideAsync(CancellationToken ct);
    }
}
