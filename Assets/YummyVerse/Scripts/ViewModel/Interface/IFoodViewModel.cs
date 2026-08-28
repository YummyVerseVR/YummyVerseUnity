using System;
using GLTFast;
using R3;
using UnityEngine;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IFoodViewModel
    {
        /// <summary>
        /// モデル自体が更新されたとき以外は発火しない
        /// </summary>
        ReactiveProperty<GltfImport> foodGltf { get; }
        ReactiveProperty<Transform> foodTransform { get; }
        
        ReactiveProperty<float>  foodScale { get; }

        event Action OnFoodResetRequested;

        /// <summary>
        /// セッション終了や救済処理から、表示中の食べ物と残量を初期状態へ戻す。
        /// </summary>
        void ResetFoodState();
    }
}
