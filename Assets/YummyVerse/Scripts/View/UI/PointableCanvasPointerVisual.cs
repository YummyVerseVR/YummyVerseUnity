using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// Draws Interaction SDK pointer positions inside a world-space canvas.
    ///
    /// OVROverlayCanvas is composited after normal scene geometry, which hides the
    /// standard controller ray cursor behind the overlay. A canvas child is included
    /// in the overlay texture itself and therefore remains visible.
    /// </summary>
    public sealed class PointableCanvasPointerVisual : MonoBehaviour
    {
        private const int TextureSize = 32;
        private const float CursorSize = 28f;

        private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color SelectColor = new Color(0.1f, 0.85f, 1f, 1f);

        private readonly Dictionary<int, CursorState> _cursors = new();

        private PointableCanvas _pointableCanvas;
        private RectTransform _canvasRect;
        private Texture2D _cursorTexture;
        private Sprite _cursorSprite;

        private sealed class CursorState
        {
            public RectTransform RectTransform;
            public Image Image;
            public bool IsSelecting;
        }

        private void Awake()
        {
            _pointableCanvas = GetComponentInParent<PointableCanvas>(true);
            if (_pointableCanvas == null || _pointableCanvas.Canvas == null)
            {
                Debug.LogWarning(
                    $"{nameof(PointableCanvasPointerVisual)} requires a parent {nameof(PointableCanvas)}.",
                    this);
                enabled = false;
                return;
            }

            _canvasRect = _pointableCanvas.Canvas.transform as RectTransform;
            CreateCursorSprite();
        }

        private void OnEnable()
        {
            if (_pointableCanvas != null)
            {
                _pointableCanvas.WhenPointerEventRaised += HandlePointerEvent;
            }
        }

        private void OnDisable()
        {
            if (_pointableCanvas != null)
            {
                _pointableCanvas.WhenPointerEventRaised -= HandlePointerEvent;
            }

            ClearCursors();
        }

        private void OnDestroy()
        {
            if (_cursorSprite != null)
            {
                Destroy(_cursorSprite);
            }

            if (_cursorTexture != null)
            {
                Destroy(_cursorTexture);
            }
        }

        private void HandlePointerEvent(PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case PointerEventType.Hover:
                {
                    var cursor = GetOrCreateCursor(pointerEvent.Identifier);
                    UpdateCursor(cursor, pointerEvent.Pose.position);
                    break;
                }
                case PointerEventType.Select:
                {
                    var cursor = GetOrCreateCursor(pointerEvent.Identifier);
                    cursor.IsSelecting = true;
                    UpdateCursor(cursor, pointerEvent.Pose.position);
                    break;
                }
                case PointerEventType.Move:
                case PointerEventType.Unselect:
                {
                    if (!_cursors.TryGetValue(pointerEvent.Identifier, out var cursor)) return;
                    cursor.IsSelecting = pointerEvent.Type != PointerEventType.Unselect
                                         && cursor.IsSelecting;
                    UpdateCursor(cursor, pointerEvent.Pose.position);
                    break;
                }
                case PointerEventType.Unhover:
                case PointerEventType.Cancel:
                    RemoveCursor(pointerEvent.Identifier);
                    break;
            }
        }

        private CursorState GetOrCreateCursor(int identifier)
        {
            if (_cursors.TryGetValue(identifier, out var cursor))
            {
                return cursor;
            }

            var cursorObject = new GameObject(
                $"UI Pointer ({identifier})",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            cursorObject.layer = _pointableCanvas.Canvas.gameObject.layer;

            var rectTransform = cursorObject.GetComponent<RectTransform>();
            rectTransform.SetParent(_canvasRect, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.one * CursorSize;

            var image = cursorObject.GetComponent<Image>();
            image.sprite = _cursorSprite;
            image.color = HoverColor;
            image.raycastTarget = false;

            cursor = new CursorState
            {
                RectTransform = rectTransform,
                Image = image
            };
            _cursors.Add(identifier, cursor);
            return cursor;
        }

        private void UpdateCursor(CursorState cursor, Vector3 worldPosition)
        {
            var localPosition = _canvasRect.InverseTransformPoint(worldPosition);
            cursor.RectTransform.anchoredPosition = new Vector2(localPosition.x, localPosition.y);
            cursor.RectTransform.SetAsLastSibling();
            cursor.Image.color = cursor.IsSelecting ? SelectColor : HoverColor;
        }

        private void RemoveCursor(int identifier)
        {
            if (!_cursors.Remove(identifier, out var cursor)) return;
            if (cursor.RectTransform != null)
            {
                Destroy(cursor.RectTransform.gameObject);
            }
        }

        private void ClearCursors()
        {
            foreach (var cursor in _cursors.Values)
            {
                if (cursor.RectTransform != null)
                {
                    Destroy(cursor.RectTransform.gameObject);
                }
            }

            _cursors.Clear();
        }

        private void CreateCursorSprite()
        {
            _cursorTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "Runtime UI Pointer",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[TextureSize * TextureSize];
            var center = (TextureSize - 1) * 0.5f;
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var outerEdge = Mathf.Clamp01(15.5f - distance);
                    var innerEdge = Mathf.Clamp01(distance - 10.5f);
                    var ringAlpha = outerEdge * innerEdge;
                    var dotAlpha = Mathf.Clamp01(2.5f - distance);
                    var alpha = (byte)(Mathf.Max(ringAlpha, dotAlpha) * 255f);
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, alpha);
                }
            }

            _cursorTexture.SetPixels32(pixels);
            _cursorTexture.Apply(false, true);
            _cursorSprite = Sprite.Create(
                _cursorTexture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f);
            _cursorSprite.name = "Runtime UI Pointer";
        }
    }
}
