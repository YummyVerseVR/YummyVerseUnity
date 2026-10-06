using System.Linq;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.Infrastructure;
using YummyVerse.Scripts.Presentation;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    public sealed class HandProgressButtonView : MonoBehaviour
    {
        private TutorialContext _context;
        private InputLayer _input;
        private IConfigUIViewModel _settings;
        private GameObject _root;
        private TextMeshProUGUI _label;
        private bool _lastVisible;
        [Inject]
        public void Construct(TutorialContext context, InputLayer input, IConfigUIViewModel settings)
        { _context = context; _input = input; _settings = settings; }

        private void Start()
        {
            _root = new GameObject("HandProgressCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.SetActive(false);
            _root.transform.SetParent(transform, false);
            var rect = (RectTransform)_root.transform;
            rect.sizeDelta = new Vector2(440, 120);
            rect.localScale = Vector3.one * 0.001f;
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 110;
            HandCanvasInteraction.Configure(canvas);
            var buttonRoot = new GameObject("ProgressButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = (RectTransform)buttonRoot.transform;
            buttonRect.SetParent(rect, false);
            buttonRect.anchorMin = Vector2.zero; buttonRect.anchorMax = Vector2.one;
            buttonRect.offsetMin = Vector2.zero; buttonRect.offsetMax = Vector2.zero;
            buttonRoot.GetComponent<Image>().color = new Color(0.12f, 0.32f, 0.48f, 1);
            buttonRoot.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_context.IsProgressButtonVisible.Value && !_settings.IsVisible.Value)
                    _input.PressStartFromHand();
            });
            var labelRoot = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)labelRoot.transform;
            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            _label = labelRoot.GetComponent<TextMeshProUGUI>();
            _label.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(f => f.name.Contains("MPLUS1p")) ?? TMP_Settings.defaultFontAsset;
            _label.fontSize = 36; _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
        }

        private void LateUpdate()
        {
            if (_root == null) return;
            var visible = _context.IsProgressButtonVisible.Value && !_settings.IsVisible.Value
                && !_context.Choice.IsVisible.Value && Camera.main != null;
            if (visible && !_lastVisible && Camera.main != null)
            {
                var camera = Camera.main;
                var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.01f) forward = camera.transform.forward;
                _root.transform.position = camera.transform.position + forward * 0.65f + Vector3.down * 0.25f;
                _root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                _root.GetComponent<Canvas>().worldCamera = camera;
                _label.text = (_context.Message.Text.Value ?? string.Empty).Contains("スタート") ? "スタート" : "次へ";
            }
            _root.SetActive(visible);
            _lastVisible = visible;
        }
    }
}

