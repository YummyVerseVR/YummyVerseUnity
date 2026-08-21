using System;
using GLTFast;
using YummyVerse.Scripts.Model.Interface;
using Zenject;
using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;

namespace YummyVerse.Scripts.ViewModel
{
    public class FoodViewModel : IFoodViewModel , IInitializable, IDisposable
    {
        private readonly IFoodContext _foodContext;
        private readonly IFoodPlacementService _foodPlacementService;
        private readonly IFoodScaleManager _foodScaleManager;
        private readonly IInputLayer _inputLayer;
        

        public ReactiveProperty<GltfImport> foodGltf { get; } = new(new());
        public ReactiveProperty<Transform> foodTransform { get; } = new();
        
        public ReactiveProperty<float> foodScale { get; } = new();
        public event Action OnFoodDestroy;
        
        private CompositeDisposable _disposables = new CompositeDisposable();

        public FoodViewModel(IFoodContext foodContext, IFoodPlacementService foodPlacementService, IFoodScaleManager foodScaleManager, IInputLayer inputLayer)
        {
            _foodContext = foodContext;
            _foodPlacementService = foodPlacementService;
            _foodScaleManager = foodScaleManager;
            _inputLayer = inputLayer;
        }

        public void Initialize()
        {
            // ダウンロードが発生したらGltfImportを更新
            _foodContext.downloadResult.Where(v => v.success).Subscribe(v =>
            {
                foodGltf.Value = v.Food.GltfImport;
            }).AddTo(_disposables);
            
            // 保存済みSpatial Anchorに対する食べ物表示位置が更新されたらtransformを更新
            _foodPlacementService.FoodTransform.Subscribe(v =>
            {
                var previous = foodTransform.Value;
                foodTransform.Value = v;
                if (ReferenceEquals(previous, v))
                {
                    foodTransform.OnNext(v);
                }
            }).AddTo(_disposables);
            
            // FoodScaleの設定値が更新されたらscaleを変更
            _foodScaleManager.FoodScale.Subscribe(v => foodScale.Value = v).AddTo(_disposables);
            
            // 食べ物破壊ボタンが押されたら食べ物破壊イベントを発火
            Observable.FromEvent(
                h => _inputLayer.OnFoodDestroyButtonClicked += h,
                h => _inputLayer.OnFoodDestroyButtonClicked -= h
                ).Subscribe(_ => OnFoodDestroy?.Invoke()).AddTo(_disposables);
        }

        public void RequestDestroyFood()
        {
            OnFoodDestroy?.Invoke();
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            foodGltf?.Dispose();
            foodTransform?.Dispose();
            foodScale?.Dispose();
        }
    }
}
