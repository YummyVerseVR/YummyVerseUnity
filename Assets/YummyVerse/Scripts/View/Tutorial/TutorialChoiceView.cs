using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.View.UI;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    /// <summary>
    /// 選択肢の提示。選択肢の数だけボタンを複製して並べる。
    /// </summary>
    public class TutorialChoiceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroupPanel panel;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private Button optionButtonPrefab;
        [SerializeField] private Transform optionsRoot;

        private readonly List<Button> _spawnedButtons = new();

        private IChoicePresenter _presenter;

        [Inject]
        public void Construct(IChoicePresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            _presenter.Prompt.Subscribe(v => promptText.text = v).AddTo(this);
            _presenter.Options.Subscribe(RebuildOptions).AddTo(this);
            _presenter.IsVisible.Subscribe(v => panel.SetVisible(v)).AddTo(this);
        }

        private void RebuildOptions(IReadOnlyList<string> labels)
        {
            foreach (var button in _spawnedButtons)
            {
                if (button != null) Destroy(button.gameObject);
            }
            _spawnedButtons.Clear();

            if (labels == null || optionButtonPrefab == null || optionsRoot == null) return;

            for (var i = 0; i < labels.Count; i++)
            {
                var index = i; // クロージャ用に確定させる
                var button = Instantiate(optionButtonPrefab, optionsRoot);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = labels[i];

                button.onClick.AddListener(() => _presenter.Select(index));
                button.gameObject.SetActive(true);
                _spawnedButtons.Add(button);
            }
        }

        private void OnDestroy()
        {
            _spawnedButtons.Clear();
        }
    }
}
