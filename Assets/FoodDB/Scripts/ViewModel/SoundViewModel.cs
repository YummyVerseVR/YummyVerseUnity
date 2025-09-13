using Food3DModel.Model;
using FoodDB.Scripts.Interface;
using R3;
using UnityEngine;
using Zenject;

namespace FoodDB.Scripts.ViewModel
{
    public class SoundViewModel: ISoundViewModel, IInitializable
    {
        [Inject] private IFoodRepositoryReader _foodRepositoryReader;
        
        public ReactiveProperty<AudioClip> ChewingSound { get; }
        
        public SoundViewModel()
        {
            ChewingSound = new ReactiveProperty<AudioClip>();
        }
        public void Initialize()
        {
            _foodRepositoryReader.ChewingSound.Where(v => v != null).Subscribe(v =>
            {
                Debug.Log("SoundViewModel: 咀嚼音の更新");
                ChewingSound.Value = v;
            });
        }
    }
}