using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using R3;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
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
        /// <summary>境界多角形から作った基準。部屋に対して絶対なので、再起動をまたいでも有効。</summary>
        public const string PlayAreaFrameKind = "playarea";

        /// <summary>
        /// 再センタリング通知だけで保っている基準。セッション内では正しいが、
        /// アプリを再起動すると起点が変わるため、保存した位置は次回の起動では使えない。
        /// </summary>
        public const string SessionFrameKind = "session";

        /// <summary>基準がこれ以上動いたら原点が張り直されたとみなしてログに残す。</summary>
        private const float MoveLogThresholdMeters = 0.01f;
        private const float RotateLogThresholdDegrees = 0.5f;

        /// <summary>境界が取れない状態が続くときに、警告を出し直す間隔 (秒)。</summary>
        private const float UnavailableLogIntervalSeconds = 10f;

        /// <summary>境界が取れるようになるのを待つ時間 (秒)。過ぎたら session 方式へ切り替える。</summary>
        private const float PlayAreaSearchSeconds = 5f;

        private readonly ReactiveProperty<bool> _isReady = new(false);
        private readonly List<Vector3> _points = new();
        private readonly List<XRInputSubsystem> _inputSubsystems = new();

        private Transform _root;
        private OVRCameraRig _rig;
        private string _source = "unknown";
        private string _lastProbe = string.Empty;
        private Pose _lastLoggedPose;
        private bool _hasLoggedPose;
        private float _nextUnavailableLogAt;
        private bool _subscribed;
        private bool _tickedOnce;
        private bool _usingPlayArea;

        /// <summary>再センタリング通知で積み上げる、部屋を保つための補正姿勢 (tracking space 内)。</summary>
        private Pose _compensatedLocalPose = new(Vector3.zero, Quaternion.identity);
        private bool _announcedSessionFrame;

        /// <summary>この時刻までは境界の立ち上がりを待つ。すぐ session に落とすと取りこぼす。</summary>
        private float _playAreaSearchDeadline;

        /// <summary>Unity 側の原点更新通知を受けている subsystem。二重購読を避けるため保持する。</summary>
        private XRInputSubsystem _subscribedSubsystem;
        /// <summary>環境レポートを出す時刻 (起動からの秒)。分岐に依存させない。</summary>
        private static readonly float[] EnvironmentReportAtSeconds = { 2f, 8f };
        private int _environmentReportIndex;
        private float _tickStartedAt;

        public Transform Current => _isReady.Value ? _root : null;
        public ReadOnlyReactiveProperty<bool> IsReady => _isReady;
        public string Kind => _usingPlayArea ? PlayAreaFrameKind : SessionFrameKind;
        public bool SurvivesRestart => _usingPlayArea;

        public void Initialize()
        {
            // 再センタリングが実際に起きているかを、SDK 側の通知でも裏取りする。
            OVRManager.TrackingOriginChangePending += OnTrackingOriginChangePending;
            _subscribed = true;

            // 構築されたことをここで宣言しておく。これが出ずに [Build] だけ出るなら、
            // 原因は DI (シーンの SceneContext / インストーラ) 側にある。
            Debug.Log("[RoomFrame] 部屋基準サービスを開始しました。次に初回 Tick を待ちます。");
        }

        public void Tick()
        {
            if (!_tickedOnce)
            {
                _tickedOnce = true;
                _tickStartedAt = Time.unscaledTime;
                _playAreaSearchDeadline = Time.unscaledTime + PlayAreaSearchSeconds;
                // これが出ずに「開始しました」で止まるなら、Zenject の Tickable が回っていない。
                Debug.Log("[RoomFrame] 初回 Tick。部屋基準の探索を開始します。");
            }

            // どの分岐に進むかに関係なく必ず出す。
            // 診断を分岐の中に置くと、分岐に入らないときに何も分からなくなる。
            ReportEnvironmentIfDue();

            var trackingSpace = ResolveTrackingSpace();
            if (trackingSpace == null)
            {
                MarkUnavailable("OVRCameraRig が見つかりません");
                return;
            }

            // 一度 session 方式に決めたら、その回はもう乗り換えない。
            // 途中で境界が取れ始めると基準が飛び、置いた食品が動いてしまう。
            if (_announcedSessionFrame)
            {
                UseRecenterCompensatedFrame(trackingSpace);
                return;
            }

            if (!TryGetPlayAreaPoints(_points))
            {
                // 起動直後は subsystem がまだ立ち上がっていないことがある。少しだけ待つ。
                if (Time.unscaledTime < _playAreaSearchDeadline)
                {
                    _isReady.Value = false;
                    return;
                }

                // 境界が取れないなら、再センタリング通知だけで基準を保つ経路へ落ちる。
                // 完全に諦めると食品がどこにも出せなくなる。
                UseRecenterCompensatedFrame(trackingSpace);
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

            _usingPlayArea = true;
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

            if (_subscribedSubsystem != null)
            {
                _subscribedSubsystem.trackingOriginUpdated -= OnUnityTrackingOriginUpdated;
                _subscribedSubsystem.boundaryChanged -= OnUnityBoundaryChanged;
                _subscribedSubsystem = null;
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

        /// <summary>
        /// 境界が取れないときの経路。トラッキング空間の原点そのものを基準にし、
        /// 再センタリングが通知されるたびにその逆変換を積んで、物理的な位置を保つ。
        ///
        /// これで守れるのはセッション内だけ。アプリを起動し直すと起点が
        /// 「そのときのトラッキング原点」に戻るため、保存済みの位置は使えない。
        /// 被り直し (アプリは動いたまま) はこれで守れる。
        /// </summary>
        private void UseRecenterCompensatedFrame(Transform trackingSpace)
        {
            _usingPlayArea = false;

            if (_root == null)
            {
                _root = new GameObject("Room Reference Frame (Session)").transform;
            }

            if (!ReferenceEquals(_root.parent, trackingSpace))
            {
                _root.SetParent(trackingSpace, false);
            }

            _root.localPosition = _compensatedLocalPose.position;
            _root.localRotation = _compensatedLocalPose.rotation;

            if (!_announcedSessionFrame)
            {
                _announcedSessionFrame = true;

                // 異常終了ではない。機能はする。運用上の制約だけを正確に伝える。
                Debug.LogWarning(
                    "[RoomFrame] session 方式で動作します (プレイエリアの境界が取得できないため)。"
                    + " 被り直しでの位置ずれは補正されます。"
                    + " ただしアプリを再起動すると配置が無効になるため、起動のたびに"
                    + " 設定画面で食品位置を置き直してください。"
                    + $" 境界の取得結果: {_lastProbe}");
            }

            _isReady.Value = true;
        }

        private void MarkUnavailable(string reason)
        {
            var wasReady = _isReady.Value;
            _isReady.Value = false;

            if (!wasReady && Time.unscaledTime < _nextUnavailableLogAt) return;

            _nextUnavailableLogAt = Time.unscaledTime + UnavailableLogIntervalSeconds;

            // 機能が成立しない状態なので Error で出す。警告が絞られている環境でも必ず見えるようにする。
            Debug.LogError(
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
            _lastProbe = string.Empty;

            var subsystem = ResolveInputSubsystem();

            // TryGetBoundaryPoints は原点モードが Floor のときしか値を返さない。
            // Stage 指定が効いていないと、境界があっても取れない。ここで揃えてから聞く。
            var mode = TrackingOriginModeFlags.Unknown;
            var supported = TrackingOriginModeFlags.Unknown;
            if (subsystem != null)
            {
                mode = subsystem.GetTrackingOriginMode();
                supported = subsystem.GetSupportedTrackingOriginModes();
                if (mode != TrackingOriginModeFlags.Floor
                    && (supported & TrackingOriginModeFlags.Floor) != 0)
                {
                    if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                    {
                        mode = subsystem.GetTrackingOriginMode();
                        Debug.Log($"[RoomFrame] 原点モードを Floor に切り替えました (現在 {mode})。");
                    }
                }
            }

            var unityOk = subsystem != null && subsystem.TryGetBoundaryPoints(destination);
            var unityCount = destination.Count;
            if (unityOk && unityCount >= 3)
            {
                _source = "UnityXR";
                return true;
            }

            destination.Clear();
            var ovrOk = TryGetOvrPlayAreaPoints(destination);
            var ovrCount = destination.Count;
            if (ovrOk && ovrCount >= 3)
            {
                _source = "OVRPlugin";
                return true;
            }

            // どちらが何を返したかを残す。ここが分からないと次の手が選べない。
            var configured = "?";
            try { configured = OVRPlugin.GetBoundaryConfigured().ToString(); }
            catch (Exception) { /* 取得できない環境では聞かない */ }

            _lastProbe =
                $"UnityXR(subsystem={(subsystem != null ? "有" : "無")}, 原点モード={mode}, 対応={supported},"
                + $" ok={unityOk}, 点数={unityCount}) / "
                + $"OVRPlugin(ok={ovrOk}, 点数={ovrCount}, BoundaryConfigured={configured})";
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
                var candidate = _inputSubsystems[i];
                if (candidate == null || !candidate.running) continue;

                if (!ReferenceEquals(candidate, _subscribedSubsystem))
                {
                    if (_subscribedSubsystem != null)
                    {
                        _subscribedSubsystem.trackingOriginUpdated -= OnUnityTrackingOriginUpdated;
                        _subscribedSubsystem.boundaryChanged -= OnUnityBoundaryChanged;
                    }

                    // Unity OpenXR では OpenXR のイベントを Unity 側が消費するため、
                    // OVRManager 経由の通知は届かない。原点が動いたことを知れるのはこちら。
                    candidate.trackingOriginUpdated += OnUnityTrackingOriginUpdated;
                    candidate.boundaryChanged += OnUnityBoundaryChanged;
                    _subscribedSubsystem = candidate;
                }

                return candidate;
            }

            return null;
        }

        /// <summary>
        /// 通知された参照空間が、いま実際に使われている原点かどうか。
        /// 判定できないときは true にする。取りこぼして補正しないより、
        /// 拾って補正する方が症状としてまし。
        /// </summary>
        /// <summary>
        /// Unity 側の原点更新通知。これが被り直しのたびに鳴るなら、原点は動いている。
        /// ただし差分は貰えないので、これだけでは補正できない (部屋に固定された実測が要る)。
        /// </summary>
        private void OnUnityTrackingOriginUpdated(XRInputSubsystem subsystem)
        {
            Debug.LogWarning(
                "[RoomFrame] Unity 通知: トラッキング原点が更新されました"
                + $" (モード={subsystem.GetTrackingOriginMode()})。"
                + " 差分は通知されないため、部屋に固定された基準が無いとこのぶんは補正できません。");
        }

        private void OnUnityBoundaryChanged(XRInputSubsystem subsystem)
        {
            Debug.Log("[RoomFrame] Unity 通知: 境界が変化しました。次の Tick で取り直します。");
        }

        /// <summary>
        /// 環境を1行にまとめて一度だけ出す。ずれの原因を人間が推測せずに決められるようにする。
        /// </summary>
        private void ReportEnvironmentIfDue()
        {
            if (_environmentReportIndex >= EnvironmentReportAtSeconds.Length) return;
            if (Time.unscaledTime - _tickStartedAt < EnvironmentReportAtSeconds[_environmentReportIndex]) return;
            _environmentReportIndex++;

            var subsystem = ResolveInputSubsystem();

            // 境界の生の取得結果も、ここで毎回取り直す。
            // 直前の分岐が何であれ、この行だけで状況が分かるようにする。
            _points.Clear();
            TryGetPlayAreaPoints(_points);

            var allowRecentering = "?";
            try { allowRecentering = OpenXRSettings.AllowRecentering.ToString(); }
            catch (Exception) { /* XR なしの実行では聞けない */ }

            var ovrOrigin = "?";
            try
            {
                if (OVRManager.instance != null) ovrOrigin = OVRManager.instance.trackingOriginType.ToString();
            }
            catch (Exception) { /* HMD 未接続では聞けない */ }

            Debug.LogWarning(
                "[RoomFrame] 環境レポート: "
                + $"AllowRecentering={allowRecentering} / "
                + $"原点モード={(subsystem != null ? subsystem.GetTrackingOriginMode().ToString() : "subsystem無")} / "
                + $"対応モード={(subsystem != null ? subsystem.GetSupportedTrackingOriginModes().ToString() : "-")} / "
                + $"OVR原点={ovrOrigin} / loadedXRDevice={OVRManager.loadedXRDevice} / "
                + $"境界={_lastProbe}");
        }

        private static bool MatchesActiveTrackingOrigin(OVRManager.TrackingOrigin origin)
        {
            var manager = OVRManager.instance;
            if (manager == null) return true;

            try
            {
                return manager.trackingOriginType == origin;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private Transform ResolveTrackingSpace()
        {
            if (_rig == null)
            {
                _rig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
            }

            return _rig != null ? _rig.trackingSpace : null;
        }

        /// <summary>
        /// 原点が張り直される直前に、新しい空間での自分の座標へ基準を移し替える。
        /// OpenXR の poseInPreviousSpace は「新しい空間の原点を、前の空間で表した姿勢」なので、
        /// 前の空間の座標 p は新しい空間では inverse(poseInPreviousSpace) * p になる。
        /// </summary>
        private void OnTrackingOriginChangePending(OVRManager.TrackingOrigin origin, OVRPose? poseInPreviousSpace)
        {
            // いま使っている参照空間の通知だけを拾う。
            // ランタイムは複数の参照空間について通知を出すため、全部に反応すると
            // 使っていない空間のぶんまで基準を動かしてしまう (二重補正)。
            if (!MatchesActiveTrackingOrigin(origin))
            {
                Debug.Log(
                    $"[RoomFrame] SDK 通知: トラッキング原点 {origin} の張り直し。"
                    + " いま使っている空間ではないので補正しません。");
                return;
            }

            if (!poseInPreviousSpace.HasValue)
            {
                Debug.LogError(
                    $"[RoomFrame] トラッキング原点 {origin} が張り直されましたが、前の空間との対応が取れません。"
                    + " このぶんのずれは補正できません。");
                return;
            }

            var delta = poseInPreviousSpace.Value;
            var inverseRotation = Quaternion.Inverse(delta.orientation);
            var inversePosition = -(inverseRotation * delta.position);

            var before = _compensatedLocalPose;
            _compensatedLocalPose = new Pose(
                inversePosition + inverseRotation * before.position,
                Quaternion.Normalize(inverseRotation * before.rotation));

            Debug.Log(
                $"[RoomFrame] SDK 通知: トラッキング原点 {origin} が張り直されます"
                + $" (pos={delta.position} yaw={delta.orientation.eulerAngles.y:F1})。"
                + $" 基準補正 {before.position} -> {_compensatedLocalPose.position}");
        }
    }
}
