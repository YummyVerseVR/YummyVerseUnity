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
        private Transform _currentPlacementTransform;

        [Inject]
        public void Construct(IFoodViewModel foodViewModel)
        {
            _foodViewModel = foodViewModel;
        }

        private void Start()
        {
            _foodAnchor = new GameObject("FoodWorldAnchor").transform;
            _foodAnchor.gameObject.SetActive(false);
            _foodRoot = new GameObject("FoodRoot");
            _foodRoot.transform.SetParent(_foodAnchor, false);
            
            // ViewModelの食べ物情報が更新されたら、食べ物を再生成
            _foodViewModel.foodGltf.SubscribeAwait(async (v, ct) =>
            {
                await InstantiateFood(v, _foodViewModel.foodTransform.Value, ct);
            }).AddTo(this);
            
            // 食べ物破壊ボタンが押されたら、食べ物を破壊
            Observable.FromEvent(
                h => _foodViewModel.OnFoodDestroy += h,
                h =>  _foodViewModel.OnFoodDestroy -= h
                ).Subscribe(_ => TryDestroyFood()).AddTo(this);
            
            
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
            TryDestroyFood();
            
            var instantiator = new GameObjectInstantiator(gltfImport, _foodRoot.transform);
            await gltfImport.InstantiateMainSceneAsync(instantiator, ct);
            
            // 食べ物のモデルが変わったときにはオブジェクトごと再生成されているため、
            // マテリアル互換性チェック・Transform設定・Scale調整を再び呼び出す。
            FoodModelVisualCompatibility.Apply(_foodRoot);
            SetFoodTransform(initialTransform);
            SetFoodScale(_foodViewModel.foodScale.Value);
        }
        
        /// <summary>
        /// Spatial Anchor配下の表示位置へ毎フレーム追従させる
        /// </summary>
        private void LateUpdate()
        {
            if (_currentPlacementTransform == null || _foodAnchor == null) return;
            _foodAnchor.SetPositionAndRotation(_currentPlacementTransform.position, _currentPlacementTransform.rotation);
        }

        private void TryDestroyFood()
        {
            if(_foodRoot  == null) return;
            Destroy(_foodRoot);
            _foodRoot = new GameObject("FoodRoot");
            _foodRoot.transform.SetParent(_foodAnchor, false);
        }

        /// <summary>
        /// 回転、座標を設定
        /// </summary>
        /// <param name="targetTransform">食べ物の回転と座標</param>
        private void SetFoodTransform(Transform targetTransform)
        {
            if(_foodRoot == null || _foodAnchor == null) return; // 食べ物の3Dモデルが未設定の場合には位置を設定できない
            if (targetTransform == null)
            {
                _currentPlacementTransform = null;
                _foodAnchor.gameObject.SetActive(false); // Anchor未設定時にワールド原点へ誤表示しない
                return;
            }

            _currentPlacementTransform = targetTransform;
            _foodAnchor.gameObject.SetActive(true);
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

    }
}
