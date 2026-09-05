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

        /// <summary>
        /// 本文の下に補助行を添えて出す。キャリブレーションのカウントダウンのように、
        /// 本文はそのままで下の行だけが変わる提示に使う。
        /// </summary>
        UniTask ShowAsync(LocalizedString msg, string subText, CancellationToken ct);

        /// <summary>表示中の本文を保ったまま補助行だけ差し替える。空文字と null は補助行なし。</summary>
        void SetSubText(string subText);

        UniTask HideAsync(CancellationToken ct);
    }
}
