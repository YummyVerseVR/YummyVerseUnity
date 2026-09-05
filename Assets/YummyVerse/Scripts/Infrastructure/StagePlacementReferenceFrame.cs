using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using R3;
using UnityEngine;
using UnityEngine.XR;
using YummyVerse.Scripts.Model.Interface;
using Zenject;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// プレイエリアの境界多角形から、物理空間に固定された基準 Transform を作る。
    ///
    /// なぜ境界多角形なのか:
    /// 境界はユーザーが実際の部屋に描いたものなので、物理的に動かない。
    /// これをトラッキング空間で毎フレーム取り直すと、
    /// 「いまのトラッキング空間から見て部屋がどこにあるか」が分かる。
    /// HMD を被り直して原点が張り直されると多角形の見え方の方が動くので、
    /// そこから作った基準はワールドの中で動き、結果として現実の同じ場所に留まる。
    ///
    /// なぜ <c>GetTrackingTransformRelativePose(Stage)</c> を使わないのか:
    /// あの API は失敗時も未対応時も <c>Posef.identity</c> を返す。
    /// 「Stage が原点と一致している」と「取得できなかった」が区別できず、
    /// 全部ゼロのまま「確立しました」と報告してしまう。判定に使ってはいけない。
    /// 境界多角形なら「点が取れたか」「何点か」で成否がはっきりする。
    ///
    /// 取れなかったときは <see cref="Current"/> を null のままにする。
    /// ここで嘘の基準を作ると、ワールド座標に食品を置いて静かにずれる。
    /// </summary>
    public sealed class StagePlacementReferenceFrame
        : IPlacementReferenceFrame, IInitializable, ITickable, IDisposable
    {
        public const string FrameKind = "playarea";

        /// <summary>基準がこれ以上動いたら原点が張り直されたとみなしてログに残す。</summary>
        private const float MoveLogThresholdMeters = 0.01f;
        private const float RotateLogThresholdDegrees = 0.5f;

        /// <summary>境界が取れない状態が続くときに、警告を出し直す間隔 (秒)。</summary>
        private const float UnavailableLogIntervalSeconds = 10f;

        private readonly ReactiveProperty<bool> _isReady = new(false);
        private readonly List<Vector3> _points = new();
        private readonly List<XRInputSubsystem> _inputSubsystems = new();

        private Transform _root;
        private OVRCameraRig _rig;
        private string _source = "unknown";
        private Pose _lastLoggedPose;
        private bool _hasLoggedPose;
        private float _nextUnavailableLogAt;
        private bool _subscribed;

        public Transform Current => _isReady.Value ? _root : null;
        public ReadOnlyReactiveProperty<bool> IsReady => _isReady;
        public string Kind => FrameKind;

        public void Initialize()
        {
            // 再センタリングが実際に起きているかを、SDK 側の通知でも裏取りする。
            OVRManager.TrackingOriginChangePending += OnTrackingOriginChangePending;
            _subscribed = true;
        }

        public void Tick()
        {
            var trackingSpace = ResolveTrackingSpace();
            if (trackingSpace == null)
            {
                MarkUnavailable("OVRCameraRig が見つかりません");
                return;
            }

            if (!TryGetPlayAreaPoints(_points))
            {
                MarkUnavailable(
                    "プレイエリアの境界を取得できません。"
                    + "ヘッドセットで境界線を歩行モードで引き直し、MQDH で境界をオフにしていないか確認してください");
                return;
            }

            if (!TryComputeFramePose(_points, out var pose))
            {
                MarkUnavailable($"境界の形から向きを決められません (点数 {_points.Count})");
                return;
            }

            if (_root == null)
            {
                _root = new GameObject("Room Reference Frame (Play Area)").transform;
            }

            if (!ReferenceEquals(_root.parent, trackingSpace))
            {
                _root.SetParent(trackingSpace, false);
            }

            _root.localPosition = pose.position;
            _root.localRotation = pose.rotation;

            LogIfMoved(pose);

            if (!_isReady.Value)
            {
                _isReady.Value = true;
                Debug.Log(
                    $"[RoomFrame] 部屋基準を確立しました (source={_source}, 境界点数={_points.Count})。"
                    + $" tracking space 内 pos={pose.position} yaw={pose.rotation.eulerAngles.y:F1}"
                    + $" / world pos={_root.position}");
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
        /// 基準がトラッキング空間の中で動いたら、それは原点が張り直された証拠。
        /// ずれが直らないときに「再センタリングが起きているのか」を数字で切り分けられるよう残す。
        /// </summary>
        private void LogIfMoved(Pose pose)
        {
            if (_hasLoggedPose
                && Vector3.Distance(pose.position, _lastLoggedPose.position) <= MoveLogThresholdMeters
                && Quaternion.Angle(pose.rotation, _lastLoggedPose.rotation) <= RotateLogThresholdDegrees)
            {
                return;
            }

            if (_hasLoggedPose)
            {
                Debug.Log(
                    "[RoomFrame] トラッキング原点が張り直されました (再センタリング検知)。"
                    + $" 部屋基準 pos {_lastLoggedPose.position} -> {pose.position}"
                    + $" / yaw {_lastLoggedPose.rotation.eulerAngles.y:F1} -> {pose.rotation.eulerAngles.y:F1}"
                    + " ぶんだけ基準を動かして追従します。");
            }

            _lastLoggedPose = pose;
            _hasLoggedPose = true;
        }

        private void MarkUnavailable(string reason)
        {
            var wasReady = _isReady.Value;
            _isReady.Value = false;

            if (!wasReady && Time.unscaledTime < _nextUnavailableLogAt) return;

            _nextUnavailableLogAt = Time.unscaledTime + UnavailableLogIntervalSeconds;
            Debug.LogWarning(
                $"[RoomFrame] 部屋基準を作れません: {reason}。"
                + "この状態では食品の位置を物理空間に固定できないため、置き場所を作りません。");
        }

        /// <summary>
        /// プレイエリアの境界点をトラッキング空間で取る。
        /// OVRBoundary は使わない。あちらは <c>loadedXRDevice == Oculus</c> でゲートされており、
        /// Unity OpenXR ローダーでは常に false / null を返す。
        /// </summary>
        private bool TryGetPlayAreaPoints(List<Vector3> destination)
        {
            destination.Clear();

            var subsystem = ResolveInputSubsystem();
            if (subsystem != null && subsystem.TryGetBoundaryPoints(destination) && destination.Count >= 3)
            {
                _source = "UnityXR";
                return true;
            }

            destination.Clear();
            if (TryGetOvrPlayAreaPoints(destination) && destination.Count >= 3)
            {
                _source = "OVRPlugin";
                return true;
            }

            return false;
        }

        /// <summary>OVRPlugin を直接叩く経路。OVRBoundary のゲートを避けるため自前で marshal する。</summary>
        private static bool TryGetOvrPlayAreaPoints(List<Vector3> destination)
        {
            try
            {
                var pointCount = 0;
                if (!OVRPlugin.GetBoundaryGeometry2(OVRPlugin.BoundaryType.PlayArea, IntPtr.Zero, ref pointCount))
                {
                    return false;
                }
                if (pointCount <= 0) return false;

                var floats = new float[pointCount * 3];
                var buffer = Marshal.AllocHGlobal(floats.Length * sizeof(float));
                try
                {
                    if (!OVRPlugin.GetBoundaryGeometry2(OVRPlugin.BoundaryType.PlayArea, buffer, ref pointCount))
                    {
                        return false;
                    }

                    Marshal.Copy(buffer, floats, 0, floats.Length);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }

                for (var i = 0; i < pointCount; i++)
                {
                    // OVRPlugin は右手系。Unity へ入れるときは Z を反転する
                    // (SDK の FromFlippedZVector3f と同じ変換)。
                    destination.Add(new Vector3(floats[3 * i], floats[3 * i + 1], -floats[3 * i + 2]));
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[RoomFrame] OVRPlugin から境界を取得できませんでした: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 境界多角形から基準姿勢を作る。位置は重心、向きは最初の辺。
        /// 「最も長い辺」のような形に依存する決め方をすると、正方形の部屋で
        /// 向きが 90 度飛ぶ危険がある。辺の順序は同じ境界なら安定している。
        /// </summary>
        private static bool TryComputeFramePose(List<Vector3> points, out Pose pose)
        {
            var centroid = Vector3.zero;
            for (var i = 0; i < points.Count; i++)
            {
                centroid += points[i];
            }
            centroid /= points.Count;

            var edge = points[1] - points[0];
            edge.y = 0f;
            if (edge.sqrMagnitude < 0.000001f)
            {
                pose = default;
                return false;
            }

            pose = new Pose(centroid, Quaternion.LookRotation(edge.normalized, Vector3.up));
            return true;
        }

        private XRInputSubsystem ResolveInputSubsystem()
        {
            _inputSubsystems.Clear();
            SubsystemManager.GetSubsystems(_inputSubsystems);

            for (var i = 0; i < _inputSubsystems.Count; i++)
            {
                if (_inputSubsystems[i] != null && _inputSubsystems[i].running)
                {
                    return _inputSubsystems[i];
                }
            }

            return null;
        }

        private Transform ResolveTrackingSpace()
        {
            if (_rig == null)
            {
                _rig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
            }

            return _rig != null ? _rig.trackingSpace : null;
        }

        private void OnTrackingOriginChangePending(OVRManager.TrackingOrigin origin, OVRPose? poseInPreviousSpace)
        {
            var delta = poseInPreviousSpace.HasValue
                ? $"pos={poseInPreviousSpace.Value.position} yaw={poseInPreviousSpace.Value.orientation.eulerAngles.y:F1}"
                : "(前の空間との対応は不明)";
            Debug.Log($"[RoomFrame] SDK 通知: トラッキング原点 {origin} が張り直されます: {delta}");
        }
    }
}
