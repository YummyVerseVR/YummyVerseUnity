using System;
using FoodDB.Scripts.Interface;
using UnityEngine;
using Zenject;

namespace Food3DModel.View
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundView: MonoBehaviour
    {
        [Inject] private ISoundViewModel _viewModel;
        
        private AudioSource _audioSource;

        public void Start()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                PlaySound();
            }
        }

        private void PlaySound()
        {
            _audioSource.clip = _viewModel.ChewingSound.Value;
            _audioSource.Play();
        }
    }
}