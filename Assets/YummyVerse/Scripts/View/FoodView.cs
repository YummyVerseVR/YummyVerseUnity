using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using Meta.WitAi;
using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View
{
    public class FoodView : MonoBehaviour
    {
        private IFoodViewModel _foodViewModel;
        [SerializeField] private bool _debugTrackableHierarchy;

        private Transform _foodAnchor;
        private GameObject _foodRoot; // シーンに生成する食べ物の3Dモデルは、このGameObjectの子として生成される。
        private Transform _currentQRTransform;

        [Inject]
        public void Construct(IFoodViewModel foodViewModel)
        {
            _foodViewModel = foodViewModel;
        }

        private void Start()
        {
            _foodAnchor = new GameObject("FoodWorldAnchor").transform;
            _foodRoot = new GameObject("FoodRoot");
            _foodRoot.transform.SetParent(_foodAnchor, false);
            
            _foodViewModel.foodGltf.SubscribeAwait(async (v, ct) =>
            {
                await InstantiateFood(v, _foodViewModel.foodTransform.Value, ct);
            }).AddTo(this);
            
            
            _foodViewModel.foodTransform.Subscribe(SetFoodTransform).AddTo(this);
            _foodViewModel.foodScale.Subscribe(SetFoodScale).AddTo(this);
        }

        /// <summary>
        /// シーンに食べ物を生成
        /// </summary>
        /// <param name="gltfImport">生成する3DモデルのGltfImport</param>
        /// <param name="initialTransform">初期座標</param>
        /// <param name="ct">CancellationToken</param>
        private async UniTask InstantiateFood(GltfImport gltfImport, Transform initialTransform, CancellationToken ct)
        {
            if (_foodRoot != null)
            {
                Destroy(_foodRoot);
                _foodRoot = new GameObject("FoodRoot");
                _foodRoot.transform.SetParent(_foodAnchor, false);
            }
            var instantiator = new GameObjectInstantiator(gltfImport, _foodRoot.transform);
            await gltfImport.InstantiateMainSceneAsync(instantiator, ct);
            
            // 食べ物のモデルが変わったときにはオブジェクトごと再生成されているため、
            // マテリアル互換性チェック・Transform設定・Scale調整を再び呼び出す。
            ApplyMaterialCompatibility();
            SetFoodTransform(initialTransform);
            SetFoodScale(_foodViewModel.foodScale.Value);
        }
        
        /// <summary>
        /// QRコードのTransformへ毎フレーム追従させる
        /// </summary>
        private void LateUpdate()
        {
            if (_currentQRTransform == null || _foodAnchor == null) return;
            _foodAnchor.SetPositionAndRotation(_currentQRTransform.position, _currentQRTransform.rotation);
        }

        /// <summary>
        /// 回転、座標を設定
        /// </summary>
        /// <param name="targetTransform">食べ物の回転と座標</param>
        private void SetFoodTransform(Transform targetTransform)
        {
            if(_foodRoot == null || _foodAnchor == null) return; // 食べ物の3Dモデルが未設定の場合には位置を設定できない
            if (targetTransform == null) return; // 初期値未設定時は座標を適用しない
            _currentQRTransform = targetTransform;
            _foodAnchor.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
            _foodRoot.transform.localPosition = Vector3.zero;
            _foodRoot.transform.localRotation = Quaternion.identity;
            _foodRoot.transform.Rotate(90,0,0);
        }
        
        /// <summary>
        /// 食べ物のスケールを変更
        /// </summary>
        /// <param name="scale">スケール</param>
        private void SetFoodScale(float scale)
        {
            if(_foodRoot == null) return;
            _foodRoot.transform.localScale = new Vector3(scale, scale, scale);
        }

        private void ApplyMaterialCompatibility()
        {
            var fallbackShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (fallbackShader == null) return;

            foreach (var renderer in _foodRoot.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                var replaced = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) continue;
                    var shader = source.shader;
                    var isUnsupported = shader == null || !shader.isSupported || shader.name == "Hidden/InternalErrorShader";
                    if (!isUnsupported) continue;

                    var target = new Material(fallbackShader);
                    CopyMaterialProperties(source, target);
                    materials[i] = target;
                    replaced = true;
                }

                if (replaced)
                {
                    renderer.materials = materials;
                }
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

        private static void CopyTextureIfPossible(Material source, Material target, string sourceProperty, string targetProperty)
        {
            if (!source.HasProperty(sourceProperty) || !target.HasProperty(targetProperty)) return;
            var texture = source.GetTexture(sourceProperty);
            if (texture == null) return;
            target.SetTexture(targetProperty, texture);
            target.SetTextureScale(targetProperty, source.GetTextureScale(sourceProperty));
            target.SetTextureOffset(targetProperty, source.GetTextureOffset(sourceProperty));
        }

        private static void CopyColorIfPossible(Material source, Material target, string sourceProperty, string targetProperty)
        {
            if (!source.HasProperty(sourceProperty) || !target.HasProperty(targetProperty)) return;
            target.SetColor(targetProperty, source.GetColor(sourceProperty));
        }

        private static void CopyFloatIfPossible(Material source, Material target, string sourceProperty, string targetProperty)
        {
            if (!source.HasProperty(sourceProperty) || !target.HasProperty(targetProperty)) return;
            target.SetFloat(targetProperty, source.GetFloat(sourceProperty));
        }

    }
}
