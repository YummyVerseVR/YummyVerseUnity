using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.Presentation;
using Zenject;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>Serialized-reference and Unity lifecycle adapter for the config UI.</summary>
    public sealed class ConfigUIView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField apiEndPointUrl;
        [SerializeField] private Button testConnectionButton;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private OVROverlayCanvas overlayCanvas;
        [SerializeField] private Slider foodScaleSlider;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform uiTransform;
        [SerializeField] private Button spatialAnchorButton;
        [SerializeField] private Button fixFoodPositionButton;
        [SerializeField] private TextMeshProUGUI spatialPlacementStatus;
        [SerializeField] private Button returnToStartButton;

        [Tooltip("入力欄に出る仮想キーボード。打鍵の途中を確定として扱わないために参照する。")]
        [SerializeField] private VirtualKeyboardView virtualKeyboard;

        private ConfigUIPresenter _presenter;

        [Inject]
        public void Construct(ConfigUIPresenter presenter)
        {
            _presenter = presenter;
        }

        private void Start()
        {
            _presenter.Initialize(
                apiEndPointUrl,
                testConnectionButton,
                canvasGroup,
                overlayCanvas,
                foodScaleSlider,
                targetCamera,
                uiTransform,
                spatialAnchorButton,
                fixFoodPositionButton,
                spatialPlacementStatus,
                returnToStartButton,
                // Unity の偽 null をインタフェースに持ち込まないよう、ここで潰しておく。
                virtualKeyboard != null ? virtualKeyboard : null);
        }

        private void OnDisable()
        {
            _presenter?.OnHostDisabled();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
