using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using YummyVerse.Scripts.View.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    /// <summary>
    /// 滞留時のヒント表示。デモ動画はループ再生する。
    /// </summary>
    public class TutorialHintView : MonoBehaviour
    {
        [SerializeField] private CanvasGroupPanel panel;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private GameObject videoRoot;

        private IHintPresenter _presenter;

        [Inject]
        public void Construct(IHintPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            if (videoPlayer != null) videoPlayer.isLooping = true;

            _presenter.Text.Subscribe(v =>
            {
                if (hintText == null) return;
                hintText.text = v;
                hintText.gameObject.SetActive(!string.IsNullOrEmpty(v));
            }).AddTo(this);

            _presenter.DemoClip.Subscribe(SetDemoClip).AddTo(this);
            _presenter.IsVisible.Subscribe(v => panel.SetVisible(v)).AddTo(this);
        }

        private void SetDemoClip(VideoClip clip)
        {
            if (videoPlayer == null) return;

            if (clip == null)
            {
                videoPlayer.Stop();
                if (videoRoot != null) videoRoot.SetActive(false);
                return;
            }

            if (videoRoot != null) videoRoot.SetActive(true);
            videoPlayer.clip = clip;
            videoPlayer.Play();
        }
    }
}
