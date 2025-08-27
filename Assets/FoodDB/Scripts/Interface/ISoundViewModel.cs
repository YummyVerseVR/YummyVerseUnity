using R3;
using UnityEngine;

namespace FoodDB.Scripts.Interface
{
    public interface ISoundViewModel
    {
        ReactiveProperty<AudioClip> ChewingSound { get; }
    }
}