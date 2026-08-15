using System.Collections;
using System.Collections.Generic;
using System.Text;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;

namespace YummyVerse.Scripts.Diagnostics
{
    /// <summary>
    /// PCVR(Windows + Quest 3 / Quest Link)への移植可否を判定するための診断コンポーネント。
    /// 空のGameObjectに貼り付けてPlayすると、Link経由で各Meta XR機能が使えるかをConsoleに出力する。
    /// </summary>
    /// <remarks>
    /// Quest LinkはWindows専用なので、macOSのEditorで実行しても意味のある結果は出ない。
    /// 検証が終わったらこのファイルごと削除して良い。
    /// </remarks>
    public class PcvrLinkDiagnostics : MonoBehaviour
    {
        [Tooltip("XRとMRUKの初期化を待つ秒数。")]
        [SerializeField] private float initialDelaySeconds = 5f;

        [Tooltip("QRコード検出状況を再出力する間隔[秒]。0以下にすると初回の1回だけ出力する。")]
        [SerializeField] private float pollIntervalSeconds = 3f;

        [Tooltip("OpenXRランタイムが公開している拡張を全件出力する。")]
        [SerializeField] private bool dumpAllExtensions;

        /// <summary>
        /// QRコードトラッキングに使われるOpenXR拡張。X1(実験的)接頭辞が付いている点に注意。
        /// </summary>
        private const string MarkerExtension = "XR_METAX1_spatial_entity_marker";

        /// <summary>
        /// 個別に有無を確認する拡張。QR以外は「Link経由でも動くはず」の裏取り用。
        /// </summary>
        private static readonly string[] InterestingExtensions =
        {
            MarkerExtension,
            "XR_FB_passthrough",
            "XR_FB_spatial_entity",
            "XR_META_spatial_entity_discovery",
            "XR_META_spatial_entity_persistence",
            "XR_EXT_hand_tracking",
        };

        /// <summary>MRUKが検出済みのトラッカブルを受け取るバッファ。</summary>
        private readonly List<MRUKTrackable> _trackables = new();

        private void Start()
        {
            StartCoroutine(RunDiagnostics());
        }

        private IEnumerator RunDiagnostics()
        {
            yield return new WaitForSeconds(initialDelaySeconds);

            Debug.Log(BuildReport());

            if (pollIntervalSeconds <= 0f)
            {
                LogTrackables();
                yield break;
            }

            var wait = new WaitForSeconds(pollIntervalSeconds);
            while (enabled)
            {
                LogTrackables();
                yield return wait;
            }
        }

        private string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== PCVR (Quest Link) Diagnostics ==========");

            sb.AppendLine("[Platform]");
            sb.AppendLine($"  Application.platform          : {Application.platform}");
            sb.AppendLine($"  XRSettings.enabled            : {XRSettings.enabled}");
            sb.AppendLine($"  XRSettings.loadedDeviceName   : '{XRSettings.loadedDeviceName}'");

            sb.AppendLine("[OpenXR Runtime]");
            sb.AppendLine($"  name                          : {OpenXRRuntime.name}");
            sb.AppendLine($"  version                       : {OpenXRRuntime.version}");
            sb.AppendLine($"  apiVersion                    : {OpenXRRuntime.apiVersion}");

            sb.AppendLine("[Headset]");
            sb.AppendLine($"  OVRPlugin.version             : {OVRPlugin.version}");
            // Link経由ならMeta_Link_Quest_3が返る。Meta_Quest_3ならスタンドアロン実行を意味する。
            sb.AppendLine($"  SystemHeadset                 : {OVRPlugin.GetSystemHeadsetType()}");

            sb.AppendLine("[Extensions]");
            foreach (var extension in InterestingExtensions)
            {
                sb.AppendLine($"  {extension,-38}: {OpenXRRuntime.IsExtensionEnabled(extension)}");
            }

            // ここが本命。3つの層(拡張/OVRPlugin/OVRAnchor)のどこで落ちているかを切り分ける。
            sb.AppendLine("[QR Code Tracking]  <-- 移植可否を決める本命");
            sb.AppendLine($"  extension enabled             : {OpenXRRuntime.IsExtensionEnabled(MarkerExtension)}");
            var markerResult = OVRPlugin.GetMarkerTrackingSupported(out var markerSupported);
            sb.AppendLine($"  GetMarkerTrackingSupported()  : result={markerResult}, supported={markerSupported}");
            sb.AppendLine($"  QRCodeTrackingSupported       : {OVRAnchor.TrackerConfiguration.QRCodeTrackingSupported}");

            sb.AppendLine("[Passthrough]");
            sb.AppendLine($"  IsInsightPassthroughSupported : {OVRManager.IsInsightPassthroughSupported()}");
            sb.AppendLine($"  IsInsightPassthroughInitialized: {OVRManager.IsInsightPassthroughInitialized()}");
            sb.AppendLine(OVRManager.instance != null
                ? $"  OVRManager.isInsightPassthroughEnabled: {OVRManager.instance.isInsightPassthroughEnabled}"
                : "  OVRManager                    : NOT FOUND (Camera Rigのあるシーンで実行すること)");

            sb.AppendLine("[Hand Tracking]");
            sb.AppendLine($"  GetHandTrackingEnabled()      : {OVRPlugin.GetHandTrackingEnabled()}");

            AppendMrukSection(sb);

            if (dumpAllExtensions)
            {
                sb.AppendLine("[All Available Extensions]");
                foreach (var extension in OpenXRRuntime.GetAvailableExtensions())
                {
                    sb.AppendLine($"  {extension}");
                }
            }

            sb.Append("==================================================");
            return sb.ToString();
        }

        private void AppendMrukSection(StringBuilder sb)
        {
            sb.AppendLine("[MRUK]");

            var mruk = MRUK.Instance;
            if (mruk == null)
            {
                sb.AppendLine("  Instance                      : NOT FOUND (MRUKプレハブのあるシーンで実行すること)");
                return;
            }

            sb.AppendLine($"  IsInitialized                 : {mruk.IsInitialized}");
            sb.AppendLine($"  Rooms                         : {mruk.GetRooms().Count}");

            // 要求した設定と実際の設定が食い違っていたら、そのトラッカブルはランタイム非対応。
            var requested = mruk.SceneSettings != null
                ? mruk.SceneSettings.TrackerConfiguration
                : default;
            sb.AppendLine($"  TrackerConfiguration requested: QRCodeTrackingEnabled={requested.QRCodeTrackingEnabled}");
            sb.AppendLine($"  TrackerConfiguration actual   : QRCodeTrackingEnabled={mruk.TrackerConfiguration.QRCodeTrackingEnabled}");
        }

        /// <summary>
        /// 実際にQRコードをカメラに映して、検出まで到達するかを確認するためのログ。
        /// </summary>
        private void LogTrackables()
        {
            var mruk = MRUK.Instance;
            if (mruk == null)
            {
                return;
            }

            mruk.GetTrackables(_trackables);
            if (_trackables.Count == 0)
            {
                Debug.Log("[PcvrLinkDiagnostics] trackables: 0 (QRコードを視野に入れてください)");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[PcvrLinkDiagnostics] trackables: {_trackables.Count}");
            foreach (var trackable in _trackables)
            {
                sb.AppendLine(
                    $"  type={trackable.TrackableType}, tracked={trackable.IsTracked}, payload='{trackable.MarkerPayloadString}'");
            }

            Debug.Log(sb.ToString());
        }
    }
}
