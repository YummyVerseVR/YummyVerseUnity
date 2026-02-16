using R3;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IStandaloneWindowViewModel
    {
        ReactiveProperty<bool> IsVisible { get; }

        void SpawnLocalFood(LocalFoods food);
    }
}