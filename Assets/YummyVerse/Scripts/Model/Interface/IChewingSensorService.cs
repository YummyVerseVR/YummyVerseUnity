using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// 咀嚼計との通信を1箇所に閉じ込めた口。
    /// COMポート探索・ハンドシェイク・再接続は実装側が常駐で面倒を見るため、
    /// 利用側は「今つながっているか」「開閉イベント」「キャリブレーション」だけを見ればよい。
    /// </summary>
    public interface IChewingSensorService
    {
        ReadOnlyReactiveProperty<ChewingSensorConnectionState> ConnectionState { get; }

        /// <summary>
        /// 開閉イベント。メインスレッドで流れる。
        /// 状態のスナップショットではなく発生の通知なので、同じ値が連続することもある。
        /// </summary>
        Observable<MouthState> OnMouthEvent { get; }

        /// <summary>
        /// キャリブレーションを1件だけ要求し、決着するまで待つ。
        ///
        /// 保留できる要求は同時に1件だけで、2件目は失敗として即座に返る (仕様書 §9.1)。
        /// 未接続や切断でも例外ではなく結果で返すので、呼び出し側は展示を止めずに続行できる。
        /// </summary>
        /// <param name="onAccepted">
        /// CAL_ACCEPTED を受信した時点で1度だけ呼ばれる。案内文の切り替え開始点として使う。
        /// </param>
        UniTask<ChewingCalibrationResult> CalibrateAsync(Action onAccepted, CancellationToken ct);
    }
}
