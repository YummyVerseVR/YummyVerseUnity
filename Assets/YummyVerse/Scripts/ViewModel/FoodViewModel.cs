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
        

        public ReactiveProperty<GltfImport> foodGltf { get; } = new(new());
        public ReactiveProperty<AudioClip> chewSound { get; } = new();
        public ReactiveProperty<Transform> foodTransform { get; } = new();
        
        public ReactiveProperty<float> foodScale { get; } = new();
        public event Action OnFoodResetRequested;
        
        private CompositeDisposable _disposables = new CompositeDisposable();

        public FoodViewModel(IFoodContext foodContext, IFoodPlacementService foodPlacementService, IFoodScaleManager foodScaleManager)
        {
            _foodContext = foodContext;
            _foodPlacementService = foodPlacementService;
            _foodScaleManager = foodScaleManager;
        }

        public void Initialize()
        {
            // ダウンロードが発生したらGltfImportを更新
            _foodContext.downloadResult.Where(v => v.success).Subscribe(v =>
            {
                foodGltf.Value = v.Food.GltfImport;
            }).AddTo(_disposables);

            // 咀嚼音は食品ごとに差し替える。失敗時とセッションリセット時は null に戻し、
            // 次の来場者へ前の食品の音を持ち越さない。
            _foodContext.downloadResult.Subscribe(v =>
            {
                chewSound.Value = v.success ? v.Food.ChewSound : null;
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
            
        }

        public void ResetFoodState()
        {
            OnFoodResetRequested?.Invoke();
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            foodGltf?.Dispose();
            chewSound?.Dispose();
            foodTransform?.Dispose();
            foodScale?.Dispose();
        }
    }
}
