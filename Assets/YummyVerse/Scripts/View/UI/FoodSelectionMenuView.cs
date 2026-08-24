using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.YummyServiceV2;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// API v2 と PersistentDataPath の食品を4列で表示するworld-space menu。
    /// previewだけを非同期取得し、GLBは選択イベントが発行されるまで取得しない。
    /// </summary>
    public sealed class FoodSelectionMenuView : MonoBehaviour, IFoodSelectionMenu, IInitializable, IDisposable
    {
        public const int ColumnCount = 4;

        private const float CanvasScale = 0.001f;
        private const float StickDeadZone = 0.18f;
        private const float StickScrollSpeed = 0.7f;

        private readonly List<Texture2D> _downloadedTextures = new();

        private GameObject _canvasRoot;
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private TextMeshProUGUI _statusText;
        private TMP_FontAsset _font;
        private Texture2D _placeholderTexture;
        private InputAction _scrollAction;
        private CancellationTokenSource _previewCts;
        private UniTaskCompletionSource<FoodCatalogItem> _selection;

        public void Initialize()
        {
            BuildUi();
            _scrollAction = new InputAction("FoodMenuScroll", InputActionType.Value);
            _scrollAction.AddBinding("<XRController>{RightHand}/primary2DAxis");
            _scrollAction.AddBinding("<Gamepad>/rightStick");
            _canvasRoot.SetActive(false);
        }

        public void ShowLoading()
        {
            EnsureReady();
            PositionInFrontOfViewer();
            ClearCards();
            _statusText.text = "API v2 と端末内の食べ物を読み込んでいます…";
            _canvasRoot.SetActive(true);
            _scrollAction.Enable();
        }

        public async UniTask<FoodCatalogItem> SelectAsync(FoodCatalogLoadResult catalog, CancellationToken ct)
        {
            EnsureReady();
            _previewCts?.Cancel();
            _previewCts?.Dispose();
            _previewCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _selection = new UniTaskCompletionSource<FoodCatalogItem>();

            RebuildCards(catalog?.Items ?? Array.Empty<FoodCatalogItem>(), _previewCts.Token);
            var count = catalog?.Items?.Count ?? 0;
            var selectableCount = catalog?.Items?.Count(item => item.IsSelectable) ?? 0;
            var status = $"{count}件（選択可能 {selectableCount}件）";
            if (!string.IsNullOrWhiteSpace(catalog?.ApiError))
            {
                status += $"  |  {catalog.ApiError}";
            }
            if (count == 0)
            {
                status += "  FoodsフォルダまたはAPI設定を確認してください。";
            }
            _statusText.text = status;

            _canvasRoot.SetActive(true);
            _scrollAction.Enable();
            PositionInFrontOfViewer();

            try
            {
                return await _selection.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                _selection = null;
            }
        }

        public void Hide()
        {
            _previewCts?.Cancel();
            _previewCts?.Dispose();
            _previewCts = null;
            _scrollAction?.Disable();
            if (_canvasRoot != null) _canvasRoot.SetActive(false);
        }

        private void Update()
        {
            if (_canvasRoot == null || !_canvasRoot.activeInHierarchy || _scrollRect == null || _scrollAction == null)
            {
                return;
            }

            var axis = _scrollAction.ReadValue<Vector2>().y;
            if (Mathf.Abs(axis) < StickDeadZone) return;

            _scrollRect.StopMovement();
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                _scrollRect.verticalNormalizedPosition + axis * StickScrollSpeed * Time.unscaledDeltaTime);
        }

        private void BuildUi()
        {
            _font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(asset => asset != null && asset.name.Contains("MPLUS1p", StringComparison.OrdinalIgnoreCase))
                ?? TMP_Settings.defaultFontAsset;
            _placeholderTexture = CreatePlaceholderTexture();

            _canvasRoot = new GameObject(
                "FoodSelectionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _canvasRoot.transform.SetParent(transform, false);

            var canvasRect = (RectTransform)_canvasRoot.transform;
            canvasRect.sizeDelta = new Vector2(1280f, 800f);
            canvasRect.localScale = Vector3.one * CanvasScale;

            var canvas = _canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            canvas.worldCamera = Camera.main;

            var scaler = _canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            ConfigurePointableCanvas(canvas);

            var panel = CreateRect("Panel", canvasRect, Vector2.zero, new Vector2(1240f, 760f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.047f, 0.075f, 0.97f);

            var title = CreateText("Title", panel, "食べ物を選んでください", 40f, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0f, 330f), new Vector2(1120f, 58f));

            _statusText = CreateText("Status", panel, string.Empty, 21f, FontStyles.Normal);
            _statusText.color = new Color(0.71f, 0.78f, 0.88f, 1f);
            SetRect(_statusText.rectTransform, new Vector2(0f, 286f), new Vector2(1120f, 36f));

            var scrollRoot = CreateRect("FoodScroll", panel, new Vector2(0f, -12f), new Vector2(1160f, 540f));
            var scrollBackground = scrollRoot.gameObject.AddComponent<Image>();
            scrollBackground.color = new Color(0.015f, 0.021f, 0.035f, 0.9f);
            _scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.inertia = true;
            _scrollRect.decelerationRate = 0.12f;
            _scrollRect.scrollSensitivity = 48f;

            var viewport = CreateStretchRect("Viewport", scrollRoot, 12f);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = CreateTopStretchRect("Content", viewport);
            var grid = _content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.cellSize = new Vector2(255f, 236f);
            grid.spacing = new Vector2(20f, 20f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ColumnCount;
            var fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.viewport = viewport;
            _scrollRect.content = _content;

            var hint = CreateText(
                "ScrollHint",
                panel,
                "右スティック ↑↓ でスクロール　／　カードをポイントしてトリガーで決定",
                20f,
                FontStyles.Normal);
            hint.color = new Color(0.62f, 0.69f, 0.8f, 1f);
            SetRect(hint.rectTransform, new Vector2(0f, -350f), new Vector2(1120f, 38f));
        }

        private void ConfigurePointableCanvas(Canvas canvas)
        {
            var collider = _canvasRoot.AddComponent<BoxCollider>();
            collider.size = new Vector3(1280f, 800f, 2f);

            var surface = _canvasRoot.AddComponent<ColliderSurface>();
            surface.InjectAllColliderSurface(collider);

            var pointableCanvas = _canvasRoot.AddComponent<PointableCanvas>();
            pointableCanvas.InjectAllPointableCanvas(canvas);

            var rayInteractable = _canvasRoot.AddComponent<RayInteractable>();
            rayInteractable.InjectAllRayInteractable(surface);
            rayInteractable.InjectOptionalPointableElement(pointableCanvas);
        }

        private void RebuildCards(IReadOnlyList<FoodCatalogItem> items, CancellationToken ct)
        {
            ClearCards();
            foreach (var item in items)
            {
                CreateCard(item, ct);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        private void CreateCard(FoodCatalogItem item, CancellationToken ct)
        {
            var card = new GameObject("FoodCard", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(_content, false);
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
                button.onClick.AddListener(() => _selection?.TrySetResult(item));
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
            rawImage.texture = _placeholderTexture;
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

            if (!string.IsNullOrWhiteSpace(item.PreviewLocation))
            {
                LoadPreviewAsync(item.PreviewLocation, rawImage, aspect, ct).Forget();
            }
        }

        private async UniTaskVoid LoadPreviewAsync(
            string location,
            RawImage target,
            AspectRatioFitter aspect,
            CancellationToken ct)
        {
            var requestLocation = location;
            if (!Uri.TryCreate(location, UriKind.Absolute, out _))
            {
                try
                {
                    requestLocation = new Uri(Path.GetFullPath(location)).AbsoluteUri;
                }
                catch (Exception exception) when (exception is ArgumentException or UriFormatException or NotSupportedException)
                {
                    return;
                }
            }

            using var request = UnityWebRequest.Get(requestLocation);
            request.timeout = 15;
            if (Uri.TryCreate(requestLocation, UriKind.Absolute, out var previewUri) &&
                (previewUri.Scheme == Uri.UriSchemeHttps || previewUri.Scheme == Uri.UriSchemeHttp))
            {
                request.SetRequestHeader(
                    "Authorization",
                    $"Bearer {YummyServiceV2Url.DevelopmentAdminToken}");
            }
            try
            {
                await request.SendWebRequest().WithCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (UnityWebRequestException)
            {
                return;
            }

            if (request.result != UnityWebRequest.Result.Success || target == null) return;
            var bytes = request.downloadHandler.data;
            if (!FoodPreviewTextureDecoder.TryDecode(bytes, out var texture)) return;

            _downloadedTextures.Add(texture);
            target.texture = texture;
            if (texture.height > 0)
            {
                aspect.aspectRatio = (float)texture.width / texture.height;
            }
        }

        private void PositionInFrontOfViewer()
        {
            var targetCamera = Camera.main;
            if (targetCamera == null) return;

            var cameraTransform = targetCamera.transform;
            var forward = cameraTransform.forward;
            _canvasRoot.transform.position = cameraTransform.position + forward * 1.45f;
            _canvasRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            _canvasRoot.transform.localScale = Vector3.one * CanvasScale;
            _canvasRoot.GetComponent<Canvas>().worldCamera = targetCamera;
        }

        private void ClearCards()
        {
            if (_content != null)
            {
                for (var index = _content.childCount - 1; index >= 0; index--)
                {
                    ReleaseObject(_content.GetChild(index).gameObject);
                }
            }

            foreach (var texture in _downloadedTextures)
            {
                ReleaseObject(texture);
            }
            _downloadedTextures.Clear();
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
            var pixels = Enumerable.Repeat(new Color(0.18f, 0.22f, 0.29f, 1f), 16).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void EnsureReady()
        {
            if (_canvasRoot == null) BuildUi();
            if (_scrollAction == null)
            {
                _scrollAction = new InputAction("FoodMenuScroll", InputActionType.Value);
                _scrollAction.AddBinding("<XRController>{RightHand}/primary2DAxis");
                _scrollAction.AddBinding("<Gamepad>/rightStick");
            }
        }

        public void Dispose()
        {
            Hide();
            ClearCards();
            _scrollAction?.Dispose();
            _scrollAction = null;
            ReleaseObject(_placeholderTexture);
            ReleaseObject(_canvasRoot);
        }

        private static void ReleaseObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
