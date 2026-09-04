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

        /// <summary>
        /// 表示中の食品の咀嚼音。用意されていない食品と、食品が出ていない間は null。
        /// null のときは ChewingSensorConfig の既定音を鳴らす。
        /// </summary>
        ReactiveProperty<AudioClip> chewSound { get; }

        ReactiveProperty<Transform> foodTransform { get; }
        
        ReactiveProperty<float>  foodScale { get; }

        /// <summary>
        /// 選択画面に入ってから食べ物が届くまで true。
        /// 表示側はこの間、食べ物の位置にフードドームを出す。
        /// </summary>
        ReadOnlyReactiveProperty<bool> isPreparing { get; }

        event Action OnFoodResetRequested;

        /// <summary>
        /// セッション終了や救済処理から、表示中の食べ物と残量を初期状態へ戻す。
        /// </summary>
        void ResetFoodState();
    }
}
