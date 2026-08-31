using System.Collections.Generic;
using UnityEngine;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// 任意の生成モデルから、当たり判定に使う Axis-Aligned Bounding Box を求める (FR18)。
    ///
    /// Q3「最も離れている2点を基準とした AABB」の解釈:
    /// 食べ物ルートのローカル座標系へ変換した全頂点集合の最小コーナーと最大コーナーの2点を採る。
    /// この2点は箱の対角線を張る「最も離れた2点」であり、各軸の extent はその差で決まる。
    /// 結果は当該座標系での通常の AABB と一致するため、モデル形状によらず必ず1つ求まる。
    ///
    /// ルートのローカル座標系で求めるので、表示位置・回転・縮小はルートの Transform が
    /// そのまま反映する。当たり判定は毎フレーム作り直さなくてよい。
    /// </summary>
    public static class FoodInteractionBoundsCalculator
    {
        /// <summary>頂点が潰れているモデルでも当たり判定が消えないようにする最小の厚み (ルートのローカル単位)。</summary>
        public const float MinimumLocalExtent = 0.01f;

        /// <summary>
        /// root 配下のメッシュから、root のローカル座標系での AABB を求める。
        /// 描画対象のメッシュが1つも無い場合は false を返し、呼び出し側は interaction ready にしないこと。
        /// </summary>
        public static bool TryCalculateLocalBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (root == null) return false;

            var points = new List<Vector3>();
            var worldToLocal = root.worldToLocalMatrix;

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                AppendMeshPoints(filter.sharedMesh, worldToLocal * filter.transform.localToWorldMatrix, points);
            }

            foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                AppendMeshPoints(skinned.sharedMesh, worldToLocal * skinned.transform.localToWorldMatrix, points);
            }

            return TryCalculateFromLocalPoints(points, out bounds);
        }

        /// <summary>
        /// ローカル座標へ変換済みの点群から AABB を求める。頂点の取得経路に依存しないのでテストしやすい。
        /// </summary>
        public static bool TryCalculateFromLocalPoints(IReadOnlyList<Vector3> localPoints, out Bounds bounds)
        {
            bounds = default;
            if (localPoints == null || localPoints.Count == 0) return false;

            var min = localPoints[0];
            var max = localPoints[0];
            for (var i = 1; i < localPoints.Count; i++)
            {
                min = Vector3.Min(min, localPoints[i]);
                max = Vector3.Max(max, localPoints[i]);
            }

            var size = max - min;
            bounds = new Bounds(
                (min + max) * 0.5f,
                new Vector3(
                    Mathf.Max(size.x, MinimumLocalExtent),
                    Mathf.Max(size.y, MinimumLocalExtent),
                    Mathf.Max(size.z, MinimumLocalExtent)));
            return true;
        }

        private static void AppendMeshPoints(Mesh mesh, Matrix4x4 meshToRoot, ICollection<Vector3> points)
        {
            if (mesh == null) return;

            // ランタイム生成モデルは読み出し不可な場合があるので、そのときはメッシュ自身の AABB の8隅で代用する。
            if (mesh.isReadable)
            {
                foreach (var vertex in mesh.vertices) points.Add(meshToRoot.MultiplyPoint3x4(vertex));
                return;
            }

            var local = mesh.bounds;
            var min = local.min;
            var max = local.max;
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            for (var z = 0; z < 2; z++)
            {
                points.Add(meshToRoot.MultiplyPoint3x4(new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z)));
            }
        }
    }
}
