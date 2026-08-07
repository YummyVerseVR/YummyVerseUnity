using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using YummyVerse.Scripts.View.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    /// <summary>
    /// 「OK!」の成功演出。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TutorialFeedbackView : MonoBehaviour
    {
        [SerializeField] private CanvasGroupPanel panel;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private RectTransform punchTarget;
        [SerializeField] private float punchScale = 0.3f;
        [SerializeField] private float punchDuration = 0.4f;

        private AudioSource _audioSource;
        private IFeedbackPresenter _presenter;

        [Inject]
        public void Construct(IFeedbackPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            _presenter.Label.Subscribe(v => labelText.text = v).AddTo(this);

            _presenter.IsVisible.Subscribe(v =>
            {
                panel.SetVisible(v);
                if (v) PlayPunch();
            }).AddTo(this);

            _presenter.OnPlaySfx.Subscribe(clip => _audioSource.PlayOneShot(clip)).AddTo(this);
        }

        private void PlayPunch()
        {
            if (punchTarget == null) return;
            punchTarget.DOKill();
            punchTarget.localScale = Vector3.one;
            punchTarget.DOPunchScale(Vector3.one * punchScale, punchDuration);
        }
    }
}
