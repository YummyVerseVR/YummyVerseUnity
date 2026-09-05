using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using Zenject;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// Stage (ガーディアン境界で定義される部屋固定の参照空間) を基準 Transform として供給する。
    ///
    /// なぜ Spatial Anchor ではなく Stage なのか:
    /// Meta の Spatial Anchor は Quest 単体向けの機能で、PCVR (Quest Link) では
    /// 作成・保存が <c>FailureUnsupported</c> で落ちうる。アンカーが作れないと
    /// 置き場所はワールド座標のまま残り、被り直しのたびに現実からずれる。
    /// Stage は OpenXR の STAGE 参照空間で、PCVR でも Quest 単体でも部屋に固定されており、
    /// アプリを再起動しても (ガーディアンを引き直さない限り) 同じ場所を指す。
    ///
    /// なぜ毎フレーム引き直すのか:
    /// <c>OVRPlugin.GetTrackingTransformRelativePose(Stage)</c> は
    /// 「Stage が、いまのトラッキング空間から見てどこにあるか」を返す。
    /// HMD の着脱で再センタリングが起きるとトラッキング空間の方が動くが、
    /// この戻り値もちょうど逆向きに動くため、毎フレーム入れ直している限り
    /// この Transform は現実の同じ場所に留まる。
    /// つまり再センタリングが起きても起きなくても正しい。
    /// </summary>
    public sealed class StagePlacementReferenceFrame
        : IPlacementReferenceFrame, IInitializable, ITickable, IDisposable
    {
        public const string FrameKind = "stage";

        private readonly ReactiveProperty<bool> _isReady = new(false);

        private Transform _root;
        private OVRCameraRig _rig;
        private bool _subscribed;
        private bool _loggedUnavailable;

        public Transform Current => _root;
        public ReadOnlyReactiveProperty<bool> IsReady => _isReady;
        public string Kind => FrameKind;

        public void Initialize()
        {
            // 再センタリングが実際に起きているかは、これが鳴るかどうかで確定できる。
            // ずれの原因を後から切り分けられるよう、必ずログに残す。
            OVRManager.TrackingOriginChangePending += OnTrackingOriginChangePending;
            _subscribed = true;
        }

        public void Tick()
        {
            var trackingSpace = ResolveTrackingSpace();
            if (trackingSpace == null)
            {
                _isReady.Value = false;
                return;
            }

            if (_root == null)
            {
                _root = new GameObject("Room Reference Frame (Stage)").transform;
            }

            if (!ReferenceEquals(_root.parent, trackingSpace))
            {
                _root.SetParent(trackingSpace, false);
            }

            var stagePose = OVRPlugin.GetTrackingTransformRelativePose(OVRPlugin.TrackingOrigin.Stage).ToOVRPose();
            _root.localPosition = stagePose.position;
            _root.localRotation = stagePose.orientation == default
                ? Quaternion.identity
                : Quaternion.Normalize(stagePose.orientation);

            if (!_isReady.Value)
            {
                _isReady.Value = true;
                Debug.Log(
                    "[RoomFrame] 部屋基準 (Stage) を確立しました。"
                    + $" tracking space からの相対 pos={_root.localPosition} rot={_root.localRotation.eulerAngles}"
                    + $" / world pos={_root.position}");
                WarnIfBoundaryMissing();
            }
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                OVRManager.TrackingOriginChangePending -= OnTrackingOriginChangePending;
                _subscribed = false;
            }

            if (_root != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_root.gameObject);
                else UnityEngine.Object.DestroyImmediate(_root.gameObject);
                _root = null;
            }

            _isReady.Dispose();
        }

        /// <summary>
        /// 再センタリングの実況。ここが鳴るなら、ワールド座標で持っている物はすべて
        /// この瞬間に物理空間からずれている。部屋基準の Transform は次の Tick で追従する。
        /// </summary>
        private void OnTrackingOriginChangePending(OVRManager.TrackingOrigin origin, OVRPose? poseInPreviousSpace)
        {
            var delta = poseInPreviousSpace.HasValue
                ? $"pos={poseInPreviousSpace.Value.position} rot={poseInPreviousSpace.Value.orientation.eulerAngles}"
                : "(前の空間との対応は不明)";
            var frameWorld = _root != null ? _root.position.ToString() : "(未確立)";
            Debug.Log(
                $"[RoomFrame] トラッキング原点 {origin} が張り直されました: {delta}"
                + $" / 直前の部屋基準 world pos={frameWorld}");
        }

        /// <summary>
        /// Stage はガーディアン(境界)で定義される。境界が無いと Stage 参照空間を作れず、
        /// <c>GetTrackingTransformRelativePose</c> は失敗しても identity を返すため
        /// 戻り値では区別がつかない。境界の有無で先回りして知らせる。
        /// </summary>
        private void WarnIfBoundaryMissing()
        {
            try
            {
                if (OVRManager.boundary == null || OVRManager.boundary.GetConfigured()) return;

                Debug.LogWarning(
                    "[RoomFrame] ヘッドセットに境界 (ガーディアン) が設定されていません。"
                    + "Stage 参照空間が定義できないため、部屋基準が実際には部屋に固定されず、"
                    + "被り直しで表示位置がずれます。境界を設定し直してください。");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[RoomFrame] 境界の設定状態を取得できませんでした: {exception.Message}");
            }
        }

        private Transform ResolveTrackingSpace()
        {
            if (_rig == null)
            {
                _rig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
            }

            if (_rig == null || _rig.trackingSpace == null)
            {
                if (!_loggedUnavailable)
                {
                    _loggedUnavailable = true;
                    Debug.LogWarning(
                        "[RoomFrame] OVRCameraRig が見つからないため部屋基準を作れません。"
                        + "この状態では食品の位置が物理空間に固定されません。");
                }
                return null;
            }

            return _rig.trackingSpace;
        }
    }
}
