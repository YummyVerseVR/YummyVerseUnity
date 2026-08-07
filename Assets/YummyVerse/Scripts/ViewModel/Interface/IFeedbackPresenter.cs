using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    /// <summary>
    /// 成功演出(「OK!」の大きめ表示)。
    /// 仕様書 S4 / S10 / S13 は独立ステップにせず、TaskStep の successFeedback として共通化している。
    /// </summary>
    public interface IFeedbackPresenter
    {
        ReactiveProperty<string> Label { get; }
        ReactiveProperty<bool> IsVisible { get; }

        /// <summary>効果音の再生要求。View 側の AudioSource が受ける。</summary>
        Observable<AudioClip> OnPlaySfx { get; }

        UniTask PlaySuccessAsync(SuccessFeedbackAsset asset, CancellationToken ct);
        UniTask HideAsync(CancellationToken ct);
    }
}
