using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;

namespace YummyVerse.Scripts.Presentation
{
    public static class HandCanvasInteraction
    {
        public static void Configure(Canvas canvas)
        {
            var root = canvas.gameObject;
            var rect = (RectTransform)canvas.transform;
            var pointable = root.GetComponent<PointableCanvas>() ?? root.AddComponent<PointableCanvas>();
            pointable.InjectAllPointableCanvas(canvas);
            var plane = root.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
            var clipper = root.AddComponent<BoundsClipper>();
            clipper.Size = new Vector3(rect.rect.width, rect.rect.height, 0.01f);
            var patch = root.AddComponent<ClippedPlaneSurface>();
            patch.InjectAllClippedPlaneSurface(plane, new IBoundsClipper[] { clipper });
            var poke = root.AddComponent<PokeInteractable>();
            poke.InjectAllPokeInteractable(patch);
            poke.InjectOptionalPointableElement(pointable);
            var ray = root.AddComponent<RayInteractable>();
            ray.InjectAllRayInteractable(patch);
            ray.InjectOptionalPointableElement(pointable);
        }
    }
}
