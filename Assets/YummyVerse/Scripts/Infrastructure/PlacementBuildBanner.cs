using UnityEngine;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// このビルドに配置基準の修正が入っているかを、DI もシーンも介さずに宣言する。
    ///
    /// 「直っていない」のか「直したコードがそもそも動いていない」のかを、
    /// 推測ではなく最初の一行で切り分けるためにある。
    /// ここが出ないビルドでは [RoomFrame] も [XrRecenter] も出るはずがない。
    /// </summary>
    public static class PlacementBuildBanner
    {
        public const string Marker = "placement-frame-v3";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LogBanner()
        {
            Debug.Log(
                $"[Build] YummyVerse 配置基準 {Marker} / platform={Application.platform}"
                + $" / unity={Application.unityVersion}");
        }
    }
}
