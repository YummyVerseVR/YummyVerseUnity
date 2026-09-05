using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// 測定フェーズの開始を利用者へ案内し、準備が整うまで送信を待たせる口 (仕様書 §9.2)。
    ///
    /// フェーズ要求を送った直後から咀嚼計は測定を始めるので、案内とカウントダウンが
    /// 終わるまで送信してはならない。その待ち合わせをここへ切り出し、通信側は
    /// 「いつ送るか」を、表示側は「何を出すか」だけを持つようにしている。
    /// </summary>
    public interface IChewingCalibrationPrompt
    {
        /// <summary>
        /// 指定フェーズの案内を出し、測定を始めてよい状態になるまで待つ。
        /// この UniTask が完了した時点で、呼び出し側が当該フェーズの要求を送信する。
        /// </summary>
        UniTask PrepareAsync(ChewingCalibrationPhase phase, CancellationToken ct);
    }
}
