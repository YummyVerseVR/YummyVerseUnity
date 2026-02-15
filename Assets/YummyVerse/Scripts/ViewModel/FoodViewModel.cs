using GLTFast;
using YummyVerse.Scripts.Model.Interface;
using Zenject;
using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;

namespace YummyVerse.Scripts.ViewModel
{
    public class FoodViewModel : IFoodViewModel , IInitializable
    {
        private readonly IFoodContext _foodContext;
        private readonly IQRDetectionService _qrDetectionService;

        public ReactiveProperty<GltfImport> foodGltf { get; } = new(new());
        public ReactiveProperty<Transform> foodTransform { get; } = new();

        public FoodViewModel(IFoodContext foodContext, IQRDetectionService qrDetectionService)
        {
            _foodContext = foodContext;
            _qrDetectionService = qrDetectionService;
        }

        public void Initialize()
        {
            // ダウンロードが発生したらGltfImportを更新
            _foodContext.downloadResult.Where(v => v.success).Subscribe(v =>
            {
                foodGltf.Value = v.Food.GltfImport;
            });
            
            // QRの位置情報が更新されたらtransformを更新
            _qrDetectionService.OnChangeTransform.Subscribe(v =>
            {
                foodTransform.OnNext(v);
            });
        }
        
    }
}