using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.XR;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// XR セッションの生死を見張り、着脱をまたいでアプリを壊さないための司令塔。
    ///
    /// PCVR (Quest Link) で HMD を外すと OpenXR セッションは STOPPING → IDLE へ落ち、
    /// 被り直すと READY → SYNCHRONIZED → FOCUSED で作り直される。
    /// このプロジェクトは Player Settings の runInBackground が有効なので、
    /// 何も手当てをしないとその間もアプリは全速で回り続け、コンポジタが居ない状態で
    /// フレームを出し続ける。再装着時に古い提出と新しいスワップチェーンが噛み合わず、
    /// メインスレッドが xrWaitFrame / xrEndFrame から戻らなくなるのがフリーズの正体である。
    ///
    /// 判定のソースを2系統持つ:
    ///   イベント (OVRManager) … 着脱を最も早く拾える。取りこぼしうるので単独では使わない。
    ///   ポーリング (Unity XR) … 毎フレームの照合。イベントを逃しても最終的に正しい値へ収束する。
    ///
    /// XR が動いていない実行 (エディタのフラット再生など) では常に Available を返す。
    ///
    /// この状態の使い道は描画負荷の上げ下げに限る。体験の進行を着脱で動かしてはいけない。
    /// 展示では headset がスタンドに置かれている時間の方が長く、着脱を在・不在の根拠にすると
    /// 「次の来場者が被った瞬間にリセットが走る」ような、運用として逆の挙動になる。
    /// 進行の判断は従来どおり IdleWatcher と入力イベントだけが担う。
    /// </summary>
    public sealed class XrSessionMonitor : IXrSessionMonitor, IInitializable, ITickable, IDisposable
    {
        private readonly XrSessionAvailabilityTracker _tracker = new();
        private readonly ReactiveProperty<XrSessionState> _state = new(XrSessionState.Available);
        private readonly List<XRDisplaySubsystem> _displays = new();

        /// <summary>OVRManager のイベントで落ちたフォーカス。ポーリングでは拾えない瞬間を埋める。</summary>
        private bool _ovrFocusLost;

        private bool _subscribed;

        public ReadOnlyReactiveProperty<XrSessionState> State => _state;

        public void Initialize()
        {
            OVRManager.HMDUnmounted += OnFocusLost;
            OVRManager.HMDLost += OnFocusLost;
            OVRManager.VrFocusLost += OnFocusLost;
            OVRManager.InputFocusLost += OnFocusLost;

            OVRManager.HMDMounted += OnFocusAcquired;
            OVRManager.HMDAcquired += OnFocusAcquired;
            OVRManager.VrFocusAcquired += OnFocusAcquired;
            OVRManager.InputFocusAcquired += OnFocusAcquired;

            _subscribed = true;
        }

        public void Tick()
        {
            var changed = _tracker.Observe(IsRuntimeUsable(), Time.unscaledTime);
            if (!changed) return;

            var next = _tracker.IsAvailable ? XrSessionState.Available : XrSessionState.Suspended;
            _state.Value = next;

            Debug.Log(next == XrSessionState.Available
                ? "[XrSession] セッションが安定しました (描画を通常へ戻します)"
                : "[XrSession] セッションを失いました (HMD の着脱 / フォーカス喪失。描画負荷を落とします)");
        }

        // ------------------------------------------------------------------
        // 判定
        // ------------------------------------------------------------------

        /// <summary>
        /// 判定は「確かに使えない証拠があるときだけ false」に倒す。
        /// 取得できない情報を根拠に保留すると、環境によっては永久に戻らなくなる。
        /// </summary>
        private bool IsRuntimeUsable()
        {
            // XR ディスプレイが1つも無い実行 (VR なしのエディタ再生、XR 初期化失敗) では
            // 保留してはいけない。ここを抜かすと、ヘッドセット無しの動作確認が永久に止まる。
            if (!TryGetDisplay(out var display)) return true;

            // イベントが最も早い。ポーリングが追いつく前でもここで落ちる。
            if (_ovrFocusLost) return false;

            if (!display.running) return false;

            // OVRManager が居ない実行 (素の OpenXR) では判定に使わない。
            if (OVRManager.instance != null && !OVRManager.isHmdPresent) return false;

            // 近接センサ。取れない実装では判定に使わない。
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.isValid
                && head.TryGetFeatureValue(CommonUsages.userPresence, out var userPresent)
                && !userPresent)
            {
                return false;
            }

            return true;
        }

        private bool TryGetDisplay(out XRDisplaySubsystem display)
        {
            _displays.Clear();
            SubsystemManager.GetSubsystems(_displays);

            for (var i = 0; i < _displays.Count; i++)
            {
                if (_displays[i] == null) continue;

                display = _displays[i];
                return true;
            }

            display = null;
            return false;
        }

        private void OnFocusLost()
        {
            _ovrFocusLost = true;

            // イベントは Tick の外で飛んでくる。次の Tick を待たずにここで落としておく。
            if (!_tracker.NotifyLost()) return;

            _state.Value = XrSessionState.Suspended;
            Debug.Log("[XrSession] セッションを失いました (HMD の着脱 / フォーカス喪失。描画負荷を落とします)");
        }

        /// <summary>
        /// 復帰は宣言するだけで、ここでは Available にしない。
        /// 実際に上げるのは <see cref="XrSessionAvailabilityTracker"/> の落ち着き待ちを
        /// 通過してからで、被り直した直後の作り直し中に仕事を積まないための間である。
        /// </summary>
        private void OnFocusAcquired() => _ovrFocusLost = false;

        public void Dispose()
        {
            if (_subscribed)
            {
                OVRManager.HMDUnmounted -= OnFocusLost;
                OVRManager.HMDLost -= OnFocusLost;
                OVRManager.VrFocusLost -= OnFocusLost;
                OVRManager.InputFocusLost -= OnFocusLost;

                OVRManager.HMDMounted -= OnFocusAcquired;
                OVRManager.HMDAcquired -= OnFocusAcquired;
                OVRManager.VrFocusAcquired -= OnFocusAcquired;
                OVRManager.InputFocusAcquired -= OnFocusAcquired;

                _subscribed = false;
            }

            _state.Dispose();
        }
    }
}
