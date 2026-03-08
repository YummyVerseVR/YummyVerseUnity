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
        private readonly IQRDetectionService _qrDetectionService;
        private readonly IFoodScaleManager _foodScaleManager;
        
        private CompositeDisposable _disposables = new CompositeDisposable();

        public ReactiveProperty<GltfImport> foodGltf { get; } = new(new());
        public ReactiveProperty<Transform> foodTransform { get; } = new();
        
        public ReactiveProperty<float> foodScale { get; } = new();

        public FoodViewModel(IFoodContext foodContext, IQRDetectionService qrDetectionService,  IFoodScaleManager foodScaleManager)
        {
            _foodContext = foodContext;
            _qrDetectionService = qrDetectionService;
            _foodScaleManager = foodScaleManager;
        }

        public void Initialize()
        {
            // ダウンロードが発生したらGltfImportを更新
            _foodContext.downloadResult.Where(v => v.success).Subscribe(v =>
            {
                foodGltf.Value = v.Food.GltfImport;
            }).AddTo(_disposables);
            
            // QRの位置情報が更新されたらtransformを更新
            _qrDetectionService.OnChangeTransform.Subscribe(v =>
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
