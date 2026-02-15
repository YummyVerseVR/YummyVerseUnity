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
    }
}