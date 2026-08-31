using UnityEngine;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>Runtime material compatibility for displayed and preview food models.</summary>
    public static class FoodModelVisualCompatibility
    {
        public static void Apply(GameObject root)
        {
            if (root == null) return;

            var fallbackShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (fallbackShader == null) return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                var replaced = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) continue;
                    var shader = source.shader;
                    var isUnsupported = shader == null
                                        || !shader.isSupported
                                        || shader.name == "Hidden/InternalErrorShader";
                    if (!isUnsupported) continue;

                    var target = new Material(fallbackShader);
                    CopyMaterialProperties(source, target);
                    materials[i] = target;
                    replaced = true;
                }

                if (replaced) renderer.materials = materials;
            }
        }

        private static void CopyMaterialProperties(Material source, Material target)
        {
            CopyColorIfPossible(source, target, "_BaseColor", "_BaseColor");
            CopyColorIfPossible(source, target, "_Color", "_BaseColor");
            CopyColorIfPossible(source, target, "_BaseColor", "_Color");
            CopyColorIfPossible(source, target, "_Color", "_Color");
            CopyTextureIfPossible(source, target, "_BaseMap", "_BaseMap");
            CopyTextureIfPossible(source, target, "_MainTex", "_BaseMap");
            CopyTextureIfPossible(source, target, "_BaseMap", "_MainTex");
            CopyTextureIfPossible(source, target, "_MainTex", "_MainTex");
            CopyFloatIfPossible(source, target, "_Metallic", "_Metallic");
            CopyFloatIfPossible(source, target, "_Smoothness", "_Smoothness");
        }

        private static void CopyTextureIfPossible(
            Material source,
            Material target,
            string sourceProperty,
            string targetProperty)
        {
            if (!source.HasProperty(sourceProperty) || !target.HasProperty(targetProperty)) return;
            var texture = source.GetTexture(sourceProperty);
            if (texture == null) return;
            target.SetTexture(targetProperty, texture);
            target.SetTextureScale(targetProperty, source.GetTextureScale(sourceProperty));
            target.SetTextureOffset(targetProperty, source.GetTextureOffset(sourceProperty));
        }

        private static void CopyColorIfPossible(
            Material source,
            Material target,
            string sourceProperty,
            string targetProperty)
        {
            if (!source.HasProperty(sourceProperty) || !target.HasProperty(targetProperty)) return;
            target.SetColor(targetProperty, source.GetColor(sourceProperty));
        }

        private static void CopyFloatIfPossible(
            Material source,
            Material target,
            string sourceProperty,
            string targetProperty)
        {
            if (!source.HasProperty(sourceProperty) || !target.HasProperty(targetProperty)) return;
            target.SetFloat(targetProperty, source.GetFloat(sourceProperty));
        }
    }
}
