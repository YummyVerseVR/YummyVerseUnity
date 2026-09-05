using System;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// 「XR ランタイムが使えるか」の生の観測を、ちらつかない可否へ均す純粋ロジック。
    ///
    /// 使えなくなった瞬間は即座に落とすが、戻ったときはすぐには上げない。
    /// Quest Link では HMD を被り直した直後にセッションの再生成とパススルーレイヤの
    /// 作り直しが走っており、そこが最も危ない。一定時間かつ一定フレーム数、
    /// 連続して使える状態が続いてから初めて可とすることで、その山場を通り過ぎるまで
    /// 描画負荷を下げたままにできる。
    ///
    /// 落ち着き待ちは、装着直後のぼやけとして見えない範囲に抑えてある (既定 0.25 秒)。
    ///
    /// Unity の API を触らないので、エディタテストからそのまま検証できる。
    /// </summary>
    public sealed class XrSessionAvailabilityTracker
    {
        /// <summary>
        /// 復帰を認めるまでに必要な連続経過時間。
        ///
        /// 描画を通常へ戻すのがこのぶんだけ遅れる = 被り直してからこの時間は解像度が低いままになる。
        /// ヘッドセットを頭に収めている最中に収まる長さにしてあり、体感では気づけない。
        /// フリーズが再発するならここを伸ばすのが最初の一手だが、伸ばすほど装着直後の
        /// ぼやけが見えるようになる。
        /// </summary>
        public const float DefaultSettleSeconds = 0.25f;

        /// <summary>復帰を認めるまでに必要な連続フレーム数。時間だけだと低フレームレート時に早すぎる。</summary>
        public const int DefaultSettleFrames = 5;

        private readonly float _settleSeconds;
        private readonly int _settleFrames;

        private bool _available = true;
        private bool _usable = true;
        private float _usableSince;
        private int _usableFrames;

        public XrSessionAvailabilityTracker(
            float settleSeconds = DefaultSettleSeconds, int settleFrames = DefaultSettleFrames)
        {
            _settleSeconds = Math.Max(0f, settleSeconds);
            _settleFrames = Math.Max(0, settleFrames);
        }

        /// <summary>通常どおり描画してよいか。</summary>
        public bool IsAvailable => _available;

        /// <summary>
        /// 1フレームぶんの観測を取り込む。毎フレーム1回だけ呼ぶこと
        /// (フレーム数の勘定をこの呼び出し回数で行っているため)。
        /// </summary>
        /// <param name="runtimeUsable">この瞬間 XR ランタイムが使えるか。</param>
        /// <param name="unscaledTime">Time.unscaledTime 相当。timeScale の影響を受けない時刻。</param>
        /// <returns>可否が変化したら true。</returns>
        public bool Observe(bool runtimeUsable, float unscaledTime)
        {
            if (!runtimeUsable) return MarkUnusable();

            if (!_usable)
            {
                // 使えない状態から戻ってきた。ここから落ち着き待ちを数え直す。
                _usable = true;
                _usableSince = unscaledTime;
                _usableFrames = 0;
            }

            _usableFrames++;

            if (_available) return false;
            if (unscaledTime - _usableSince < _settleSeconds) return false;
            if (_usableFrames < _settleFrames) return false;

            _available = true;
            return true;
        }

        /// <summary>
        /// イベント経由で「今この瞬間に使えなくなった」と分かったときに呼ぶ。
        /// 次の Observe を待たずに落とすためのもので、ポーリングの取りこぼしも埋める。
        /// </summary>
        /// <returns>可否が変化したら true。</returns>
        public bool NotifyLost() => MarkUnusable();


        private bool MarkUnusable()
        {
            _usable = false;
            _usableFrames = 0;

            if (!_available) return false;

            _available = false;
            return true;
        }
    }
}
