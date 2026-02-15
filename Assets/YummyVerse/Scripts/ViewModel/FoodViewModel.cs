using GLTFast;
using YummyVerse.Scripts.Model.Interface;
using Zenject;
using R3
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel
{
    public class FoodViewModel : IFoodViewModel , IInitializable
    {
        private readonly IFoodContext _foodContext;
        private readonly IQRDetectionService _qrDetectionService;

        public ReactiveProperty<GltfImport> foodGltf { get; } = new();
        public ReactiveProperty<Transform> foodTransform { get; } = new();

        public FoodViewModel(IFoodContext foodContext, IQRDetectionService qrDetectionService)
        {
            _foodContext = foodContext;
            _qrDetectionService = qrDetectionService;
        }

        public void Initialize()
        {
            _foodContext.downloadResult.Where(v => v.success).Subscribe(v =>
            {
                foodGltf.Value = v.Food.GltfImport;
                foodTransform.Value = _qrDetectionService.OnDetected.Value.transform;
            });
        }
        
    }
}