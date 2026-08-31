using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// Coordinates menu UI, preview loading, and the single active selection request.
    /// It is a plain C# lifetime owner; the MonoBehaviour only forwards callbacks.
    /// </summary>
    public sealed class FoodSelectionMenuPresenter : IDisposable
    {
        private const float StickDeadZone = 0.18f;
        private const float StickScrollSpeed = 0.7f;
        private const float DisplayDistance = 1.45f;
        private const float CanvasScale = 0.001f;

        private readonly FoodSelectionMenuUiBuilder _uiBuilder;
        private readonly FoodPreviewLoader _previewLoader;
        private readonly List<Texture2D> _downloadedTextures = new();
        private FoodSelectionMenuUi _ui;
        private CancellationTokenSource _previewCancellation;
        private UniTaskCompletionSource<FoodCatalogItem> _selection;
        private bool _initialized;
        private bool _disposed;

        public FoodSelectionMenuPresenter(
            FoodSelectionMenuUiBuilder uiBuilder,
            FoodPreviewLoader previewLoader)
        {
            _uiBuilder = uiBuilder ?? throw new ArgumentNullException(nameof(uiBuilder));
            _previewLoader = previewLoader ?? throw new ArgumentNullException(nameof(previewLoader));
        }

        public void Initialize(Transform owner)
        {
            if (_initialized || _disposed) return;
            _initialized = true;
            _ui = _uiBuilder.Build(owner, SelectItem);
        }

        public void ShowLoading()
        {
            EnsureInitialized();
            CancelPreviews();
            ReleaseTextures();
            _uiBuilder.ClearCards();
            _ui.StatusText.text = "API v2 と端末内の食べ物を読み込んでいます…";
            ShowRoot();
        }

        public async UniTask<FoodCatalogItem> SelectAsync(
            FoodCatalogLoadResult catalog,
            CancellationToken cancellationToken)
        {
            EnsureInitialized();
            CancelPreviews();
            ReleaseTextures();
            _previewCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _selection = new UniTaskCompletionSource<FoodCatalogItem>();

            var items = catalog?.Items ?? Array.Empty<FoodCatalogItem>();
            var cards = _uiBuilder.CreateCards(items, SelectItem);
            var status = $"{items.Count}件（選択可能 {items.Count(item => item.IsSelectable)}件）";
            if (!string.IsNullOrWhiteSpace(catalog?.ApiError)) status += $"  |  {catalog.ApiError}";
            if (items.Count == 0) status += "  FoodsフォルダまたはAPI設定を確認してください。";
            _ui.StatusText.text = status;
            ShowRoot();

            foreach (var card in cards)
            {
                if (!string.IsNullOrWhiteSpace(card.Item.PreviewLocation))
                {
                    LoadPreviewAsync(card, _previewCancellation.Token).Forget();
                }
            }

            try
            {
                return await _selection.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                _selection = null;
            }
        }

        public void Hide()
        {
            CancelPreviews();
            ReleaseTextures();
            _ui?.ScrollAction?.Disable();
            if (_ui?.Root != null) _ui.Root.SetActive(false);
        }

        public void Tick(float deltaTime)
        {
            if (_ui?.Root == null || !_ui.Root.activeInHierarchy || _ui.ScrollRect == null) return;
            var axis = _ui.ScrollAction?.ReadValue<Vector2>().y ?? 0f;
            if (Mathf.Abs(axis) < StickDeadZone) return;

            _ui.ScrollRect.StopMovement();
            _ui.ScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                _ui.ScrollRect.verticalNormalizedPosition
                + axis * StickScrollSpeed * deltaTime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Hide();
            _uiBuilder.Dispose();
            _ui = null;
        }

        private async UniTaskVoid LoadPreviewAsync(
            FoodSelectionMenuCard card,
            CancellationToken cancellationToken)
        {
            try
            {
                var texture = await _previewLoader.LoadAsync(
                    card.Item.PreviewLocation,
                    cancellationToken);
                if (texture == null || card.Preview == null)
                {
                    if (texture != null) DestroyTexture(texture);
                    return;
                }

                _downloadedTextures.Add(texture);
                card.Preview.texture = texture;
                if (texture.height > 0)
                {
                    card.Aspect.aspectRatio = (float)texture.width / texture.height;
                }
            }
            catch (OperationCanceledException)
            {
                // The menu was hidden or the owning object was destroyed.
            }
        }

        private void SelectItem(FoodCatalogItem item)
        {
            if (item != null && item.IsSelectable) _selection?.TrySetResult(item);
        }

        private void ShowRoot()
        {
            PositionInFrontOfViewer();
            _ui.Root.SetActive(true);
            _ui.ScrollAction.Enable();
        }

        private void PositionInFrontOfViewer()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var cameraTransform = camera.transform;
            _ui.Root.transform.position = cameraTransform.position
                                          + cameraTransform.forward * DisplayDistance;
            _ui.Root.transform.rotation = Quaternion.LookRotation(
                cameraTransform.forward,
                Vector3.up);
            _ui.Root.transform.localScale = Vector3.one * CanvasScale;
            _ui.Canvas.worldCamera = camera;
        }

        private void EnsureInitialized()
        {
            if (!_initialized) throw new InvalidOperationException("FoodSelectionMenuPresenter must be initialized first.");
        }

        private void CancelPreviews()
        {
            if (_previewCancellation == null) return;
            _previewCancellation.Cancel();
            _previewCancellation.Dispose();
            _previewCancellation = null;
        }

        private void ReleaseTextures()
        {
            foreach (var texture in _downloadedTextures) DestroyTexture(texture);
            _downloadedTextures.Clear();
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
            else UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
