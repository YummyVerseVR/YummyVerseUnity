using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    public sealed class FoodSelectionMenuUi
    {
        public GameObject Root { get; set; }
        public Canvas Canvas { get; set; }
        public ScrollRect ScrollRect { get; set; }
        public RectTransform Content { get; set; }
        public TextMeshProUGUI StatusText { get; set; }
        public InputAction ScrollAction { get; set; }
        public Texture2D PlaceholderTexture { get; set; }
    }

    public sealed class FoodSelectionMenuCard
    {
        public FoodCatalogItem Item { get; set; }
        public RawImage Preview { get; set; }
        public AspectRatioFitter Aspect { get; set; }
    }

    /// <summary>
    /// Constructs and owns the menu's uGUI object graph. It knows nothing about catalog
    /// loading or preview transport; it only maps catalog records to selectable cards.
    /// </summary>
    public sealed class FoodSelectionMenuUiBuilder : IDisposable
    {
        private const float CanvasScale = 0.001f;
        private const int ColumnCount = 4;

        private TMP_FontAsset _font;
        private FoodSelectionMenuUi _ui;

        public FoodSelectionMenuUi Build(Transform owner, Action<FoodCatalogItem> onSelected)
        {
            if (_ui != null) return _ui;

            _font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                         .FirstOrDefault(asset => asset != null
                             && asset.name.Contains("MPLUS1p", StringComparison.OrdinalIgnoreCase))
                     ?? TMP_Settings.defaultFontAsset;

            _ui = new FoodSelectionMenuUi
            {
                PlaceholderTexture = CreatePlaceholderTexture()
            };
            _ui.Root = new GameObject(
                "FoodSelectionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _ui.Root.transform.SetParent(owner, false);

            var canvasRect = (RectTransform)_ui.Root.transform;
            canvasRect.sizeDelta = new Vector2(1280f, 800f);
            canvasRect.localScale = Vector3.one * CanvasScale;

            _ui.Canvas = _ui.Root.GetComponent<Canvas>();
            _ui.Canvas.renderMode = RenderMode.WorldSpace;
            _ui.Canvas.sortingOrder = 100;
            _ui.Canvas.worldCamera = Camera.main;

            var scaler = _ui.Root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            ConfigurePointableCanvas();
            BuildLayout(canvasRect);

            _ui.ScrollAction = new InputAction("FoodMenuScroll", InputActionType.Value);
            _ui.ScrollAction.AddBinding("<XRController>{RightHand}/primary2DAxis");
            _ui.ScrollAction.AddBinding("<Gamepad>/rightStick");
            _ui.Root.SetActive(false);
            return _ui;
        }

        public IReadOnlyList<FoodSelectionMenuCard> CreateCards(
            IReadOnlyList<FoodCatalogItem> items,
            Action<FoodCatalogItem> onSelected)
        {
            if (_ui == null) throw new InvalidOperationException("Menu UI must be built before cards are created.");
            ClearCards();

            var cards = new List<FoodSelectionMenuCard>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    cards.Add(CreateCard(item, onSelected));
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_ui.Content);
            _ui.ScrollRect.verticalNormalizedPosition = 1f;
            return cards;
        }

        public void ClearCards()
        {
            if (_ui?.Content == null) return;
            for (var index = _ui.Content.childCount - 1; index >= 0; index--)
            {
                ReleaseObject(_ui.Content.GetChild(index).gameObject);
            }
        }

        public void Dispose()
        {
            if (_ui == null) return;
            _ui.ScrollAction?.Dispose();
            ClearCards();
            ReleaseObject(_ui.PlaceholderTexture);
            ReleaseObject(_ui.Root);
            _ui = null;
        }

        private void BuildLayout(RectTransform canvasRect)
        {
            var panel = CreateRect("Panel", canvasRect, Vector2.zero, new Vector2(1240f, 760f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.047f, 0.075f, 0.97f);

            var title = CreateText("Title", panel, "食べ物を選んでください", 40f, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0f, 330f), new Vector2(1120f, 58f));

            _ui.StatusText = CreateText("Status", panel, string.Empty, 21f, FontStyles.Normal);
            _ui.StatusText.color = new Color(0.71f, 0.78f, 0.88f, 1f);
            SetRect(_ui.StatusText.rectTransform, new Vector2(0f, 286f), new Vector2(1120f, 36f));

            var scrollRoot = CreateRect("FoodScroll", panel, new Vector2(0f, -12f), new Vector2(1160f, 540f));
            var scrollBackground = scrollRoot.gameObject.AddComponent<Image>();
            scrollBackground.color = new Color(0.015f, 0.021f, 0.035f, 0.9f);
            _ui.ScrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            _ui.ScrollRect.horizontal = false;
            _ui.ScrollRect.vertical = true;
            _ui.ScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _ui.ScrollRect.inertia = true;
            _ui.ScrollRect.decelerationRate = 0.12f;
            _ui.ScrollRect.scrollSensitivity = 48f;

            var viewport = CreateStretchRect("Viewport", scrollRoot, 12f);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.gameObject.AddComponent<RectMask2D>();

            _ui.Content = CreateTopStretchRect("Content", viewport);
            var grid = _ui.Content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.cellSize = new Vector2(255f, 236f);
            grid.spacing = new Vector2(20f, 20f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ColumnCount;
            var fitter = _ui.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _ui.ScrollRect.viewport = viewport;
            _ui.ScrollRect.content = _ui.Content;

            var hint = CreateText(
                "ScrollHint",
                panel,
                "右スティック ↑↓ でスクロール　／　カードをポイントしてトリガーで決定",
                20f,
                FontStyles.Normal);
            hint.color = new Color(0.62f, 0.69f, 0.8f, 1f);
            SetRect(hint.rectTransform, new Vector2(0f, -350f), new Vector2(1120f, 38f));
        }

        private void ConfigurePointableCanvas()
        {
            var collider = _ui.Root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1280f, 800f, 2f);
            var surface = _ui.Root.AddComponent<ColliderSurface>();
            surface.InjectAllColliderSurface(collider);
            var pointableCanvas = _ui.Root.AddComponent<PointableCanvas>();
            pointableCanvas.InjectAllPointableCanvas(_ui.Canvas);
            var rayInteractable = _ui.Root.AddComponent<RayInteractable>();
            rayInteractable.InjectAllRayInteractable(surface);
            rayInteractable.InjectOptionalPointableElement(pointableCanvas);
        }

        private FoodSelectionMenuCard CreateCard(
            FoodCatalogItem item,
            Action<FoodCatalogItem> onSelected)
        {
            var card = new GameObject("FoodCard", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(_ui.Content, false);
            var background = card.GetComponent<Image>();
            background.color = item.IsSelectable
                ? new Color(0.12f, 0.16f, 0.24f, 1f)
                : new Color(0.08f, 0.09f, 0.12f, 0.85f);

            var button = card.GetComponent<Button>();
            button.interactable = item.IsSelectable;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.73f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.47f, 0.75f, 1f, 1f);
            colors.disabledColor = new Color(0.52f, 0.52f, 0.52f, 0.62f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (item.IsSelectable)
            {
                button.onClick.AddListener(() => onSelected?.Invoke(item));
            }

            var previewFrame = CreateRect(
                "PreviewFrame",
                (RectTransform)card.transform,
                new Vector2(0f, 26f),
                new Vector2(225f, 164f));
            var frameImage = previewFrame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(0.025f, 0.031f, 0.047f, 1f);
            frameImage.raycastTarget = false;

            var preview = CreateStretchRect("Preview", previewFrame, 4f);
            var rawImage = preview.gameObject.AddComponent<RawImage>();
            rawImage.texture = _ui.PlaceholderTexture;
            rawImage.color = item.IsSelectable ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1f);
            rawImage.raycastTarget = false;
            var aspect = preview.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 4f / 3f;

            var labelValue = item.IsSelectable ? item.DisplayName : $"{item.DisplayName}\n（準備中）";
            var label = CreateText("FoodName", (RectTransform)card.transform, labelValue, 24f, FontStyles.Bold);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 24f;
            SetRect(label.rectTransform, new Vector2(0f, -84f), new Vector2(225f, 56f));

            return new FoodSelectionMenuCard
            {
                Item = item,
                Preview = rawImage,
                Aspect = aspect
            };
        }

        private TextMeshProUGUI CreateText(
            string objectName,
            RectTransform parent,
            string value,
            float fontSize,
            FontStyles style)
        {
            var rect = CreateRect(objectName, parent, Vector2.zero, Vector2.zero);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(
            string objectName,
            RectTransform parent,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, anchoredPosition, size);
            return rect;
        }

        private static RectTransform CreateStretchRect(string objectName, RectTransform parent, float inset)
        {
            var rect = CreateRect(objectName, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        private static RectTransform CreateTopStretchRect(string objectName, RectTransform parent)
        {
            var rect = CreateRect(objectName, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Texture2D CreatePlaceholderTexture()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "FoodPreviewPlaceholder",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = Enumerable.Repeat(
                new Color(0.18f, 0.22f, 0.29f, 1f),
                16).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void ReleaseObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
