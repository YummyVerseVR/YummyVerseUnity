using System;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// ダウンロード中の「準備中」を伝えるフードドームを1つだけ持ち、表示/非表示を切り替える表示コラボレーター。
    ///
    /// - 食べ物と同じ anchor の子として生成し、位置だけ食べ物に合わせる。
    ///   姿勢は取っ手が上を向くようワールド無回転で固定する
    ///   (食べ物の 3D モデルとフードドームで既定の姿勢が異なるため、anchor の回転には追従させない)。
    /// - モデルが未設定でも動作は止めない。1度だけ警告を出し、ドーム無しで進む。
    /// </summary>
    public sealed class FoodDomeController : IDisposable
    {
        private readonly FoodRevealSettings _settings;
        private GameObject _dome;
        private bool _disposed;

        public FoodDomeController(FoodRevealSettings settings, Transform parent)
        {
            _settings = settings ?? new FoodRevealSettings();
            _dome = Build(parent);
        }

        public bool IsVisible => _dome != null && _dome.activeSelf;

        public void SetVisible(bool visible)
        {
            if (_disposed || _dome == null) return;
            _dome.SetActive(visible);
        }

        /// <summary>食べ物の位置に合わせ直す。姿勢はワールド無回転のまま動かさない。</summary>
        public void SyncPose(Vector3 foodPosition)
        {
            if (_disposed || _dome == null) return;

            _dome.transform.SetPositionAndRotation(
                foodPosition + Vector3.up * _settings.DomeHeightOffset,
                Quaternion.identity);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DestroyObject(_dome);
            _dome = null;
        }

        private GameObject Build(Transform parent)
        {
            if (_settings.DomePrefab == null)
            {
                Debug.LogWarning(
                    "[Food] フードドームのモデルが未設定のため、ダウンロード中は何も表示されません。"
                    + "FoodView の Reveal Settings の Dome Prefab に "
                    + "Assets/YummyVerse/Prefabs/Restaurant/FoodDoom.glb を割り当ててください。");
                return null;
            }

            // 親の回転は SyncPose で打ち消すため、ここでは大きさだけ決めておく。
            var dome = UnityEngine.Object.Instantiate(_settings.DomePrefab, parent, false);
            dome.name = "FoodDome";
            dome.transform.localScale = Vector3.one * _settings.DomeScale;

            // すくい判定はモデルの Collider を拾うため、ドームが食べ物に化けないよう当たり判定は落とす。
            foreach (var collider in dome.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            dome.SetActive(false);
            return dome;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
