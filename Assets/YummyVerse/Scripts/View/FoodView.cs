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

        private void LateUpdate()
        {
            if (!_debugTrackableHierarchy) return;
            var trackable = _foodViewModel.foodTransform.Value;
            if (trackable != null)
            {
                Debug.Log($"[QR] name:{trackable.name} parent:{trackable.parent?.name} root:{trackable.root.name}");
            }

            if (_foodRoot != null)
            {
                var foodTransform = _foodRoot.transform;
                Debug.Log($"[FOOD] parent:{foodTransform.parent?.name} root:{foodTransform.root.name}");
            }
        }

        /// <summary>
        /// 回転、座標を設定
        /// </summary>
        /// <param name="targetTransform">食べ物の回転と座標</param>
        private void SetFoodTransform(Transform targetTransform)
        {
            if(_foodRoot == null || _foodAnchor == null) return; // 食べ物の3Dモデルが未設定の場合には位置を設定できない
            if (targetTransform == null) return; // 初期値未設定時は座標を適用しない
            _foodAnchor.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
            _foodRoot.transform.localPosition = Vector3.zero;
            _foodRoot.transform.localRotation = Quaternion.identity;
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
            ApplyMaterialFallback();
            SetFoodTransform(initialTransform);
        }

        private void ApplyMaterialFallback()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var standard = Shader.Find("Standard");
            var fallbackShader = urpLit != null ? urpLit : standard;
            if (fallbackShader == null) return;

            foreach (var renderer in _foodRoot.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                var hasChanges = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || material.shader == null || material.shader.isSupported) continue;
                    material.shader = fallbackShader;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    renderer.materials = materials;
                }
            }
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
    }
}
