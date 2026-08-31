using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.Video;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// 滞留した来場者へのヒント提示(テキスト強調 + デモ動画ループ)。
    /// </summary>
    public interface IHintPresenter
    {
        ReactiveProperty<string> Text { get; }
        ReactiveProperty<VideoClip> DemoClip { get; }
        ReactiveProperty<bool> IsVisible { get; }

        UniTask ShowAsync(HintPresentation hint, CancellationToken ct);
        UniTask HideAsync(CancellationToken ct);
    }
}
