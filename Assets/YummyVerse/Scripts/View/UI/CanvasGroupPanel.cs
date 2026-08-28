using DG.Tweening;
using UnityEngine;
using YummyVerse.Scripts.Presentation;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// CanvasGroup のフェード表示/非表示と、カメラ前への配置をまとめた部品。
    /// ConfigUIView / StandaloneWindowView などに4回コピペされている処理と同じ挙動。
    /// </summary>
    public class CanvasGroupPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeDuration = 0.1f;

        [Header("カメラ前に配置する場合のみ設定")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform uiTransform;
        [SerializeField] private float displayDistanceFromCamera = 0.6f;
        [SerializeField] private bool followCameraOnShow;

        private PointableCanvasInteractionGate _interactionGate;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            uiTransform = transform;
        }

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _interactionGate = new PointableCanvasInteractionGate(this);
        }

        public void SetVisible(bool visible)
        {
            if (visible) Show();
            else Hide();
        }

        public void Show()
        {
            if (canvasGroup == null) return;
            if (followCameraOnShow) PlaceInFrontOfCamera();

            canvasGroup.DOFade(1, fadeDuration);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            _interactionGate?.SetEnabled(true);
        }

        public void Hide()
        {
            if (canvasGroup == null) return;

            canvasGroup.DOFade(0, fadeDuration);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 見えていないパネルがコントローラのレイを奪うと、表示中のダイアログを押せなくなる。
            _interactionGate?.SetEnabled(false);
        }

        public void PlaceInFrontOfCamera()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || uiTransform == null)
            {
                Debug.LogWarning($"{name}: targetCamera または uiTransform が未設定です。", this);
                return;
            }

            var cameraTransform = targetCamera.transform;
            uiTransform.position = cameraTransform.position + cameraTransform.forward * displayDistanceFromCamera;
            uiTransform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
        }
    }
}
