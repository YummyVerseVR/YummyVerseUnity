using System;
using R3;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IStandaloneWindowViewModel
    {
        ReactiveProperty<bool> IsVisible { get; }

        /// <summary>Changes visibility without exposing state mutation to application callers.</summary>
        void SetVisible(bool isVisible);

        void SpawnLocalFood(LocalFoods food);

        /// <summary>
        /// ローカル食品のスポーンが成立したときに発火する。
        /// チュートリアル側はこれを「メニューが選ばれた」として購読する。
        /// </summary>
        event Action<LocalFoods> OnLocalFoodSpawned;
    }
}
