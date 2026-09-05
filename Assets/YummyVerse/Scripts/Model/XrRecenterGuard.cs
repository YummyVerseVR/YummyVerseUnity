using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// トラッキング原点を物理空間に固定し続ける番人。
    ///
    /// Quest は HMD を被り直すとランタイムから再センタリング要求を出す。これを受け入れると
    /// OpenXR のワールド原点が「いまの頭の位置」へ張り直され、Unity のワールド座標系が
    /// 部屋に対してまるごと動く。設定画面で決めた食品の位置も、Spatial Anchor から
    /// 計算した位置も、この瞬間に現実に対してずれる。
    /// (Meta SDK の <c>OVRSpatialAnchor</c> はアンカーの姿勢を OVRPlugin のトラッキング空間で
    ///  取得し、Unity 側のカメラ姿勢を使ってワールドへ変換する。再センタリングで両者の
    ///  基準がずれると、アンカーに貼ってある物まで一緒にずれる。)
    ///
    /// 対策は再センタリングそのものを起こさせないこと。原点を Stage
    /// (OpenXR の STAGE 参照空間。部屋に固定され、再センタリングされない) に倒し、
    /// OVRManager と OpenXR の双方で再センタリングを切る。
    /// Meta SDK 側の対応は <c>OVRManager.trackingOriginType</c> の setter にあり、
    /// Stage は「Floor + 再センタリング off」へマップされる。
    ///
    /// 設定はシーンの OVRManager にも入れてあるが、Building Block のプレハブ更新などで
    /// 戻りうる値なので、実行時にもここで押さえ直す。ずれてからでは展示中に直せない。
    /// </summary>
    public sealed class XrRecenterGuard : IInitializable, ITickable, IDisposable
    {
        /// <summary>HMD 不在などで適用できない間、毎フレーム試し続けないための間隔 (秒)。</summary>
        private const float RetryIntervalSeconds = 1f;

        private bool _applied;
        private bool _subscribed;
        private bool _warned;

        /// <summary>OpenXR のネイティブが居ない実行。何度試しても無駄なので粘らない。</summary>
        private bool _openXrUnavailable;
        private float _nextAttemptAt;

        public void Initialize()
        {
            // 被り直した直後こそ再センタリングが飛んでくる。その度に押さえ直す。
            OVRManager.HMDMounted += OnHmdMounted;
            OVRManager.VrFocusAcquired += OnHmdMounted;
            _subscribed = true;

            Apply();
        }

        public void Tick()
        {
            if (_applied || _openXrUnavailable || Time.unscaledTime < _nextAttemptAt) return;
            Apply();
        }

        public void Dispose()
        {
            if (!_subscribed) return;

            OVRManager.HMDMounted -= OnHmdMounted;
            OVRManager.VrFocusAcquired -= OnHmdMounted;
            _subscribed = false;
        }

        private void OnHmdMounted()
        {
            _applied = false;
            _nextAttemptAt = 0f;
        }

        private void Apply()
        {
            _nextAttemptAt = Time.unscaledTime + RetryIntervalSeconds;

            // OpenXR 側は OVRManager を経由せずに直接落とす。
            // OVRManager が Stage を適用できない環境でも、ここだけは効かせておく。
            if (!TrySetOpenXrRecentering(false)) return;

            var manager = OVRManager.instance;
            if (manager == null) return;

            // OVRManager は shouldRecenter を見て自前でも RecenterPose() を呼ぶ。そちらも止める。
            manager.AllowRecenter = false;

            // HMD が繋がるまで origin は設定できない。次の試行に回す。
            if (!OVRManager.isHmdPresent) return;

            // Meta SDK は Stage を「Floor + 再センタリング off」へマップする。
            manager.trackingOriginType = OVRManager.TrackingOrigin.Stage;

            // 効いたかどうかは OpenXR 側の再センタリング設定で見る。
            // OVRManager の getter は OVRPlugin 側の値を返し、UnityOpenXR 経由では
            // Stage を Floor と報告しうるので、判定に使うと永久に不一致のままになる。
            if (!TryGetOpenXrRecentering(out var recenteringAllowed)) return;

            _applied = !recenteringAllowed;
            if (_applied)
            {
                // 「再センタリング要求を受け付けない」ことしか保証できない。
                // 原点が実際に部屋へ固定されたかは、ここでは分からない。
                // 実際にずれていないかの判定は [RoomFrame] のログで行うこと。
                Debug.Log(
                    "[XrRecenter] 再センタリングを無効化しました "
                    + $"(OpenXR AllowRecentering=false / OVRManager 原点={manager.trackingOriginType})。");
                return;
            }

            if (_warned) return;
            _warned = true;
            Debug.LogWarning(
                "[XrRecenter] 再センタリングを止められませんでした。"
                + "この状態では HMD を被り直すと表示位置が物理空間に対してずれます。"
                + "ガーディアン(境界)が未設定だと STAGE 参照空間を作れないことがあります。");
        }

        /// <summary>
        /// OpenXR の再センタリング設定を切り替える。XR が動いていない実行
        /// (エディタのフラット再生など) ではネイティブが居ないので、そこで止まらないようにする。
        /// </summary>
        private bool TrySetOpenXrRecentering(bool allow)
        {
            try
            {
                OpenXRSettings.SetAllowRecentering(allow);
                return true;
            }
            catch (Exception exception)
            {
                MarkOpenXrUnavailable(exception);
                return false;
            }
        }

        private bool TryGetOpenXrRecentering(out bool allowed)
        {
            try
            {
                allowed = OpenXRSettings.AllowRecentering;
                return true;
            }
            catch (Exception exception)
            {
                MarkOpenXrUnavailable(exception);
                allowed = false;
                return false;
            }
        }

        private void MarkOpenXrUnavailable(Exception exception)
        {
            _openXrUnavailable = true;
            Debug.LogWarning(
                "[XrRecenter] OpenXR の再センタリング設定に触れませんでした "
                + $"(XR なしの実行では想定内): {exception.Message}");
        }
    }
}
