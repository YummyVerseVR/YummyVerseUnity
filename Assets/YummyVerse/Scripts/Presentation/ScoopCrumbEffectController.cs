using System;
using UnityEngine;
using UnityEngine.Rendering;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// 食べかすの ParticleSystem を1つだけ持ち、すくいのたびに手動で Emit する表示コラボレーター。
    ///
    /// - Prefab を必要としないよう、ParticleSystem は設定値から実行時に組み立てる。
    ///   Inspector で Material を割り当てればそれを優先する。
    /// - simulationSpace は World。食べ物の縮小や皿の追従に粒が引きずられないようにするため、
    ///   食べ物の階層ではなく独立した GameObject に置く。
    /// - rate 0 の loop で再生し続け、Emit だけで噴き出す。Play() の呼び直しでは
    ///   再生中のバーストが取りこぼされるため、この形にしている。
    /// </summary>
    public sealed class ScoopCrumbEffectController : IScoopCrumbEffect, IDisposable
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

        private readonly ScoopCrumbEffectSettings _settings;
        private ParticleSystem _particles;
        private Material _generatedMaterial;
        private bool _disposed;

        public ScoopCrumbEffectController(ScoopCrumbEffectSettings settings)
        {
            _settings = settings ?? new ScoopCrumbEffectSettings();
            _particles = Build();
        }

        public void Play(Vector3 position, Vector3 direction)
        {
            if (_disposed || _particles == null) return;

            var rotation = direction.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(direction)
                : Quaternion.identity;
            _particles.transform.SetPositionAndRotation(position, rotation);

            if (!_particles.isPlaying) _particles.Play();
            _particles.Emit(UnityEngine.Random.Range(_settings.MinCount, _settings.MaxCount + 1));
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
            var host = new GameObject("ScoopCrumbEffect");
            var particles = host.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                _settings.MinLifetimeSeconds, _settings.MaxLifetimeSeconds);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_settings.MinSpeed, _settings.MaxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(_settings.MinSize, _settings.MaxSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(_settings.DarkColor, _settings.LightColor);
            main.gravityModifier = _settings.GravityModifier;
            main.maxParticles = MaxParticles;
            main.stopAction = ParticleSystemStopAction.None;

            // 噴き出しは Emit() だけで行う。自動発生は止めておく。
            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;
            emission.SetBursts(Array.Empty<ParticleSystem.Burst>());

            // Cone は transform の +Z へ向かって広がる。Play() で向きを合わせる。
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = _settings.SpreadAngleDegrees;
            shape.radius = Mathf.Max(_settings.SpawnRadius, 0.0001f);
            shape.radiusThickness = 1f;
            shape.arc = 360f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.65f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            // 不透明なマテリアルを割り当てられても消え際が見えるよう、大きさでも絞る。
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.7f, 0.9f),
                    new Keyframe(1f, 0f)));

            var rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

            var renderer = host.GetComponent<ParticleSystemRenderer>()
                           ?? host.AddComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.None;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = ResolveMaterial();

            particles.Play();
            return particles;
        }

        private Material ResolveMaterial()
        {
            if (_settings.Material != null) return _settings.Material;

            foreach (var shaderName in ShaderCandidates)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null) continue;

                _generatedMaterial = new Material(shader) { name = "ScoopCrumb (generated)" };
                MakeTransparent(_generatedMaterial);
                return _generatedMaterial;
            }

            Debug.LogWarning(
                "[Eating] 食べかす用のシェーダーが見つかりませんでした。"
                + "FoodView の Crumb Effect Settings の Material にマテリアルを割り当ててください。");
            return null;
        }

        /// <summary>消え際のフェードが出るよう、生成したマテリアルだけ半透明に倒す。</summary>
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
