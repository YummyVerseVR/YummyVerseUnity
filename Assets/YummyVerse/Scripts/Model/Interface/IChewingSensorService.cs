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
        ///
        /// ノイズ測定と咀嚼測定の順序、再送、タイムアウトは実装側が持つ。呼び出し側は
        /// <paramref name="prompt"/> で「いつ測定を始めてよいか」だけを答える (仕様書 §9)。
        /// </summary>
        /// <param name="prompt">
        /// 各フェーズの要求を送る直前に呼ばれる案内。完了するまでフェーズ要求を送らない。
        /// </param>
        UniTask<ChewingCalibrationResult> CalibrateAsync(IChewingCalibrationPrompt prompt, CancellationToken ct);
    }
}
