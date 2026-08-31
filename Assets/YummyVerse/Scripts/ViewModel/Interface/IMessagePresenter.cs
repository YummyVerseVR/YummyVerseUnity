using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.Localization;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// 説明文の提示。ステップが UI コンポーネントを直接触ることを禁止するための境界。
    /// 状態は ReactiveProperty で公開し、View は購読するだけにする(ConfigUIViewModel と同じ流儀)。
    /// </summary>
    public interface IMessagePresenter
    {
        ReactiveProperty<string> Text { get; }
        ReactiveProperty<bool> IsVisible { get; }

        UniTask ShowAsync(LocalizedString msg, CancellationToken ct);
        UniTask HideAsync(CancellationToken ct);
    }
}
