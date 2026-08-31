using R3;
using TMPro;
using UnityEngine;
using YummyVerse.Scripts.View.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    public class TutorialMessageView : MonoBehaviour
    {
        [SerializeField] private CanvasGroupPanel panel;
        [SerializeField] private TextMeshProUGUI messageText;

        private IMessagePresenter _presenter;

        [Inject]
        public void Construct(IMessagePresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            _presenter.Text.Subscribe(v => messageText.text = v).AddTo(this);
            _presenter.IsVisible.Subscribe(v => panel.SetVisible(v)).AddTo(this);
        }
    }
}
