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

        event Action OnFoodDestroy;

        /// <summary>
        /// ボタン入力以外(救済のForceComplete、セッションリセット)から食べ物の破棄を依頼する。
        /// </summary>
        void RequestDestroyFood();
    }
}
