using R3;
using UnityEngine;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    /// <summary>
    /// ナレーション音声の再生。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TutorialVoiceView : MonoBehaviour
    {
        private AudioSource _audioSource;
        private IVoicePresenter _presenter;

        [Inject]
        public void Construct(IVoicePresenter presenter)
        {
            _presenter = presenter;
        }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            _presenter.OnPlay.Subscribe(clip =>
            {
                _audioSource.Stop();
                _audioSource.clip = clip;
                _audioSource.Play();
            }).AddTo(this);

            _presenter.OnStop.Subscribe(_ => _audioSource.Stop()).AddTo(this);
        }
    }
}
