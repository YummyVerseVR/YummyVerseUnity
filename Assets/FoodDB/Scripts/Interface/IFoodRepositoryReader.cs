using R3;
using UnityEngine;

namespace Food3DModel.Model
{
    public interface IFoodRepositoryReader
    {
        ReactiveProperty<GameObject> Food3DModel { get; }
        ReactiveProperty<AudioClip> ChewingSound { get; } 
    }
}