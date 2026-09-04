using System;
using UnityEngine;
using UnityEngine.Rendering;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// ドームが消えて食べ物が現れる瞬間に出す、白い煙の ParticleSystem を1つだけ持つ表示コラボレーター。
    ///
    /// - Prefab を必要としないよう、設定値から実行時に組み立てる (食べかすの ScoopCrumbEffectController と同じ方針)。
    /// - loop させず duration = 演出時間 で 1 サイクルだけ再生する。3回のバーストに分けて出すことで、
    ///   一瞬の破裂ではなく指定時間ぶん立ちのぼり続ける煙になる。
    /// - simulationSpace は World。食べ物や皿の動きに粒が引きずられないよう、独立した GameObject に置く。
    /// </summary>
    public sealed class FoodRevealSmokeEffect : IDisposable
    {
        private const int MaxParticles = 256;

        /// <summary>粒ごとの色 (vertex color) が効くものを優先した、実行時マテリアルの候補。</summary>
        private static readonly string[] ShaderCandidates =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Sprites/Default",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
        };

        private readonly FoodRevealSettings _settings;
        private ParticleSystem _particles;
        private Material _generatedMaterial;
        private bool _disposed;

        public FoodRevealSmokeEffect(FoodRevealSettings settings)
        {
            _settings = settings ?? new FoodRevealSettings();
            _particles = Build();
        }

        /// <summary>指定位置で煙を頭から再生し直す。</summary>
        public void Play(Vector3 position)
        {
            if (_disposed || _particles == null) return;

            _particles.transform.position = position;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particles.Play(true);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_particles != null) DestroyObject(_particles.gameObject);
            _particles = null;

            if (_generatedMaterial != null) DestroyObject(_generatedMaterial);
            _generatedMaterial = null;
        }

        private ParticleSystem Build()
        {
            var host = new GameObject("FoodRevealSmoke");

            // Cone は transform の +Z へ広がる。上向きに固定し、位置だけを毎回動かす。
            host.transform.rotation = Quaternion.LookRotation(Vector3.up);
            var particles = host.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var duration = Mathf.Max(_settings.SmokeDurationSeconds, 0.01f);

            var main = particles.main;
            main.loop = false;
            main.duration = duration;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                _settings.MinLifetimeSeconds, _settings.MaxLifetimeSeconds);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_settings.MinSpeed, _settings.MaxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(_settings.MinSize, _settings.MaxSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(_settings.SmokeColor);
            main.gravityModifier = _settings.GravityModifier;
            main.maxParticles = MaxParticles;
            main.stopAction = ParticleSystemStopAction.None;

            // 演出時間いっぱい煙が出続けるよう、総量を3回に分けて出す。
            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;
            emission.SetBursts(new[]
            {
                CreateBurst(0f, 0.5f),
                CreateBurst(duration * 0.35f, 0.3f),
                CreateBurst(duration * 0.7f, 0.2f),
            });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = _settings.SpreadAngleDegrees;
            shape.radius = Mathf.Max(_settings.SpawnRadius, 0.0001f);
            shape.radiusThickness = 1f;
            shape.arc = 360f;

            // 湧いた瞬間に濃く、消え際にふわりと薄くなる。
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            // 煙らしく、時間とともにふくらみながら消える。
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.6f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(1f, 1.4f)));

            var rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var renderer = host.GetComponent<ParticleSystemRenderer>()
                           ?? host.AddComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.None;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = ResolveMaterial();

            return particles;
        }

        private ParticleSystem.Burst CreateBurst(float time, float share)
        {
            var min = (short)Mathf.Max(Mathf.RoundToInt(_settings.MinCount * share), 1);
            var max = (short)Mathf.Max(Mathf.RoundToInt(_settings.MaxCount * share), min);
            return new ParticleSystem.Burst(time, min, max);
        }

        private Material ResolveMaterial()
        {
            if (_settings.Material != null) return _settings.Material;

            foreach (var shaderName in ShaderCandidates)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null) continue;

                _generatedMaterial = new Material(shader) { name = "FoodRevealSmoke (generated)" };
                MakeTransparent(_generatedMaterial);
                return _generatedMaterial;
            }

            Debug.LogWarning(
                "[Food] 白い煙用のシェーダーが見つかりませんでした。"
                + "FoodView の Reveal Settings の Material にマテリアルを割り当ててください。");
            return null;
        }

        /// <summary>煙が透けるよう、生成したマテリアルだけ半透明に倒す。</summary>
        private static void MakeTransparent(Material material)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
