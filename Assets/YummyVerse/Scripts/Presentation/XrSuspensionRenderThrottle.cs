using System;
using R3;
using UnityEngine;
using UnityEngine.XR;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// 保留中は描画の解像度を落として、セッション作り直しの余裕を作る。
    ///
    /// HMD を外している間はどのみち誰も見ていない。それでも Player Settings の
    /// runInBackground が有効な以上フレームは回り続けるので、せめて1フレームあたりの
    /// 仕事を減らしておく。被り直した瞬間に GPU が飽和していないことが、
    /// ランタイム側のレイヤ再生成と噛み合うための余裕になる。
    ///
    /// カメラを止める手もあるが、URP で有効なカメラが0になると
    /// 「No cameras rendering」が毎フレーム出てログ側で詰まるため採らない。
    /// renderViewportScale は XR プラグインが無視しても実害が無い。
    /// </summary>
    public sealed class XrSuspensionRenderThrottle : IInitializable, IDisposable
    {
        /// <summary>
        /// 保留中の描画スケール。面積比で約1/10になる。
        ///
        /// 下げている間は誰も被っていない。元へ戻すのは落ち着き待ちを抜けた後なので、
        /// 被り直してから 0.25 秒ほどは低解像度のままだが、ヘッドセットを頭に収めている
        /// 最中に収まるので体感では気づけない。
        /// フリーズが再発するならここを下げる (または落ち着き待ちを伸ばす)。
        /// </summary>
        private const float SuspendedViewportScale = 0.3f;

        private readonly IXrSessionMonitor _xrSession;
        private readonly CompositeDisposable _disposables = new();

        private bool _throttled;
        private float _restoreScale = 1f;

        public XrSuspensionRenderThrottle(IXrSessionMonitor xrSession)
        {
            _xrSession = xrSession;
        }

        public void Initialize()
        {
            _xrSession.State
                .Subscribe(state =>
                {
                    if (state == XrSessionState.Suspended) Throttle();
                    else Restore();
                })
                .AddTo(_disposables);
        }

        private void Throttle()
        {
            if (_throttled) return;
            if (!XRSettings.enabled) return;

            _restoreScale = Mathf.Clamp(XRSettings.renderViewportScale, 0.1f, 1f);
            XRSettings.renderViewportScale = SuspendedViewportScale;
            _throttled = true;
        }

        private void Restore()
        {
            if (!_throttled) return;

            _throttled = false;
            if (!XRSettings.enabled) return;

            XRSettings.renderViewportScale = _restoreScale;
        }

        public void Dispose()
        {
            Restore();
            _disposables.Dispose();
        }
    }
}
