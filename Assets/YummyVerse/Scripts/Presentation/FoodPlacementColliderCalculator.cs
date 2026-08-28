using UnityEngine;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>Builds the interaction box from the rendered preview geometry.</summary>
    public static class FoodPlacementColliderCalculator
    {
        public const float MinimumColliderSize = 0.05f;

        public static bool TryApply(BoxCollider collider, Transform marker)
        {
            if (collider == null || marker == null) return false;

            var renderers = marker.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;

            var localBounds = new Bounds(
                marker.InverseTransformPoint(renderers[0].bounds.center),
                Vector3.zero);
            foreach (var renderer in renderers)
            {
                EncapsulateWorldBounds(ref localBounds, marker, renderer.bounds);
            }

            collider.center = localBounds.center;
            collider.size = new Vector3(
                Mathf.Max(localBounds.size.x, MinimumColliderSize),
                Mathf.Max(localBounds.size.y, MinimumColliderSize),
                Mathf.Max(localBounds.size.z, MinimumColliderSize));
            return true;
        }

        private static void EncapsulateWorldBounds(
            ref Bounds target,
            Transform localSpace,
            Bounds worldBounds)
        {
            var min = worldBounds.min;
            var max = worldBounds.max;
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var z = 0; z < 2; z++)
                    {
                        target.Encapsulate(localSpace.InverseTransformPoint(new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z)));
                    }
                }
            }
        }
    }
}
