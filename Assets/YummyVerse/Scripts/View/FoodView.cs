using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View
{
    public class FoodView : MonoBehaviour
    {
        /// <summary>食事による縮小をこの速さで補間する。段階的に小さくなるのを見せるための演出 (FR21)。</summary>
        private const float ConsumptionShrinkSpeed = 4f;

        /// <summary>完食後、見た目が十分小さくなったら食べ物を破棄する閾値 (FR22)。</summary>
        private const float DisappearThreshold = 0.02f;

        private IFoodViewModel _foodViewModel;
        private IFoodEatingService _foodEatingService;
        private IScoopProbeProvider _scoopProbeProvider;
        private IScoopHaptics _scoopHaptics;

        [SerializeField] private bool _debugTrackableHierarchy;
        [SerializeField] private ScoopDetectionSettings _scoopSettings = new();

        private Transform _foodAnchor;
        private GameObject _foodRoot; // シーンに生成する食べ物の3Dモデルは、このGameObjectの子として生成される。
        private Transform _currentPlacementTransform;

        // 表示スケールは「運営が設定した基準スケール × 食事による残量倍率」で決まる。
        // 設定値を食事で書き換えてしまうと次の来場者に持ち越されるため、両者は必ず別に持つ。
        private float _baseScale = 1f;
        private float _targetConsumptionScale = 1f;
        private float _currentConsumptionScale = 1f;

        [Inject]
        public void Construct(
            IFoodViewModel foodViewModel,
            IFoodEatingService foodEatingService,
            IScoopProbeProvider scoopProbeProvider,
            IScoopHaptics scoopHaptics)
        {
            _foodViewModel = foodViewModel;
            _foodEatingService = foodEatingService;
            _scoopProbeProvider = scoopProbeProvider;
            _scoopHaptics = scoopHaptics;
        }

        private void Start()
        {
            _foodAnchor = new GameObject("FoodWorldAnchor").transform;
            _foodAnchor.gameObject.SetActive(false);
            _foodRoot = new GameObject("FoodRoot");
            _foodRoot.transform.SetParent(_foodAnchor, false);
            _baseScale = _foodViewModel.foodScale.Value;

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
            _foodViewModel.foodScale.Subscribe(SetBaseScale).AddTo(this);

            // すくうたびに残量が減る。見た目の縮小はここから駆動する。
            _foodEatingService.RemainingFraction
                .Subscribe(v => _targetConsumptionScale = v).AddTo(this);
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
            var instantiated = await gltfImport.InstantiateMainSceneAsync(instantiator, ct);
            
            // 食べ物のモデルが変わったときにはオブジェクトごと再生成されているため、
            // マテリアル互換性チェック・Transform設定・Scale調整を再び呼び出す。
            FoodModelVisualCompatibility.Apply(_foodRoot);
            SetFoodTransform(initialTransform);
            ApplyScale();

            // 起動直後の空の GltfImport でも購読が走るため、実体があるときだけ食べられる状態にする。
            if (instantiated && _foodRoot.transform.childCount > 0) SetUpScoopInteraction();
        }

        /// <summary>
        /// 表示中のモデルから当たり判定を作り、食べられる状態にする。
        /// 形状を取得できないモデルは interaction ready にせず、原因が分かる警告を残す (FR18)。
        /// </summary>
        private void SetUpScoopInteraction()
        {
            if (_foodRoot == null) return;

            var target = _foodRoot.AddComponent<FoodScoopTargetView>();
            if (!target.TryInitialize(_foodEatingService, _scoopProbeProvider, _scoopHaptics, _scoopSettings))
            {
                Destroy(target);
                _foodEatingService.AbandonFood();
                Debug.LogWarning("[Eating] モデルの形状を取得できないため、当たり判定を作れませんでした。この食べ物はすくえません。");
                return;
            }

            _foodEatingService.BeginFood();
        }
        
        /// <summary>
        /// Spatial Anchor配下の表示位置へ毎フレーム追従させる
        /// </summary>
        private void LateUpdate()
        {
            UpdateConsumptionScale();

            if (_currentPlacementTransform == null || _foodAnchor == null) return;
            _foodAnchor.SetPositionAndRotation(_currentPlacementTransform.position, _currentPlacementTransform.rotation);
        }

        /// <summary>
        /// 残量に向けて表示スケールを詰め、完食しきったら食べ物を消す (FR21, FR22)。
        /// </summary>
        private void UpdateConsumptionScale()
        {
            if (Mathf.Approximately(_currentConsumptionScale, _targetConsumptionScale))
            {
                TryDisappearWhenCleared();
                return;
            }

            _currentConsumptionScale = Mathf.MoveTowards(
                _currentConsumptionScale,
                _targetConsumptionScale,
                ConsumptionShrinkSpeed * Time.deltaTime);
            ApplyScale();
            TryDisappearWhenCleared();
        }

        private void TryDisappearWhenCleared()
        {
            if (_targetConsumptionScale > 0f) return;
            if (_currentConsumptionScale > DisappearThreshold) return;
            if (_foodRoot == null || _foodRoot.GetComponent<FoodScoopTargetView>() == null) return;

            TryDestroyFood();
        }

        private void TryDestroyFood()
        {
            if(_foodRoot  == null) return;
            Destroy(_foodRoot);
            _foodRoot = new GameObject("FoodRoot");
            _foodRoot.transform.SetParent(_foodAnchor, false);

            // 破棄は完食とは限らない(破棄ボタン・セッションリセット)ので、DishCleared は出さない。
            _foodEatingService.AbandonFood();
            _currentConsumptionScale = 1f;
            _targetConsumptionScale = 1f;
            ApplyScale();
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
        /// 運営が設定した基準スケールを変更する
        /// </summary>
        /// <param name="scale">スケール</param>
        private void SetBaseScale(float scale)
        {
            _baseScale = scale;
            ApplyScale();
        }

        private void ApplyScale()
        {
            if(_foodRoot == null) return;
            _foodRoot.transform.localScale = Vector3.one * (_baseScale * _currentConsumptionScale);
        }

    }
}
