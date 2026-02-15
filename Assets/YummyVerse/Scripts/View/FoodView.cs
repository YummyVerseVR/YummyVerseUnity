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
        
        private GameObject _foodRoot; // シーンに生成する食べ物の3Dモデルは、このGameObjectの子として生成される。

        [Inject]
        public void Construct(IFoodViewModel foodViewModel)
        {
            _foodViewModel = foodViewModel;
        }

        private void Start()
        {
            _foodRoot = new GameObject("FoodRoot");
            _foodViewModel.foodGltf.SubscribeAwait(async (v, ct) =>
            {
                await InstantiateFood(v, _foodViewModel.foodTransform.Value, ct);
            });
            _foodViewModel.foodTransform.Subscribe(SetFoodTransform);
        }

        /// <summary>
        /// 回転、座標を設定
        /// </summary>
        /// <param name="targetTransform">食べ物の回転と座標</param>
        private void SetFoodTransform(Transform targetTransform)
        {
            if(_foodRoot == null) return; // 食べ物の3Dモデルが未設定の場合には位置を設定できない
            if (targetTransform == null) return; // 初期値未設定時は座標を適用しない
            _foodRoot.transform.position = targetTransform.position;
            _foodRoot.transform.rotation = targetTransform.rotation;
            Debug.Log("transformを設定");
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
            }
            var instantiator = new GameObjectInstantiator(gltfImport, _foodRoot.transform);
            await gltfImport.InstantiateMainSceneAsync(instantiator, ct);
            SetFoodTransform(initialTransform);
            Debug.Log("モデルを再生成");
        }
        
    }
}
