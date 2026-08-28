using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// 入力欄にフォーカスが入っている間だけ Meta のバーチャルキーボード
    /// (<see cref="OVRVirtualKeyboard"/>) を表示する。
    /// </summary>
    /// <remarks>
    /// Quest 単体で使うシステムキーボード(TouchScreenKeyboard)は PC ビルドでは
    /// 一切使えないため、PCVR(Quest Link)でも動くバーチャルキーボードに寄せている。
    /// ランタイム側が XR_META_virtual_keyboard を出していない環境では
    /// OVRVirtualKeyboard 側が警告を出して何も表示されない。その場合でも
    /// TMP_InputField は物理キーボードから入力できる。
    /// </remarks>
    public sealed class VirtualKeyboardView : MonoBehaviour
    {
        /// <summary>Meta XR SDK の OVRVirtualKeyboard プレハブ。</summary>
        [SerializeField] private OVRVirtualKeyboard keyboardPrefab;

        /// <summary>キーボードを出す対象の入力欄。</summary>
        [SerializeField] private TMP_InputField inputField;

        /// <summary>配置の基準にするトランスフォーム。通常は設定画面のルート。</summary>
        [SerializeField] private Transform placementAnchor;

        /// <summary>
        /// Custom は placementAnchor の下に自前で置く。Far / Near はランタイムに任せる。
        /// </summary>
        [Header("Placement")]
        [SerializeField]
        private OVRVirtualKeyboard.KeyboardPosition positionMode = OVRVirtualKeyboard.KeyboardPosition.Custom;

        [Tooltip("Custom のとき、基準トランスフォームからどれだけ下に置くか[m]。パネルと重なるとレイが取られるので、パネル下端(約0.2m)より下に置く。")]
        [SerializeField] private float verticalOffset = 0.35f;

        [Tooltip("Custom のとき、基準トランスフォームからどれだけ手前に置くか[m]。")]
        [SerializeField] private float nearOffset = 0.1f;

        [Tooltip("Custom のとき、キーボードを何度手前に倒すか[deg]。")]
        [SerializeField] private float tiltAngle = 30f;

        private OVRVirtualKeyboard _keyboard;
        private TMPVirtualKeyboardTextHandler _textHandler;
        private readonly List<GameObject> _interactorAnchors = new();

        private void Awake()
        {
            if (inputField == null || keyboardPrefab == null)
            {
                Debug.LogWarning($"{nameof(VirtualKeyboardView)}: inputField / keyboardPrefab が未設定です。", this);
                enabled = false;
                return;
            }

            // 入力手段をバーチャルキーボードに一本化する。
            // (Android 実機ではこれを切らないと OS のシステムキーボードも同時に出る)
            inputField.shouldHideSoftKeyboard = true;
        }

        private void OnEnable()
        {
            if (inputField == null) return;
            inputField.onSelect.AddListener(HandleSelect);
            inputField.onDeselect.AddListener(HandleDeselect);
        }

        private void OnDisable()
        {
            if (inputField != null)
            {
                inputField.onSelect.RemoveListener(HandleSelect);
                inputField.onDeselect.RemoveListener(HandleDeselect);
            }

            Hide();
        }

        private void OnDestroy()
        {
            // OVRVirtualKeyboard はシングルトンなので、使い終わったら必ず片付ける。
            if (_keyboard != null)
            {
                Destroy(_keyboard.gameObject);
                _keyboard = null;
            }

            // コントローラー配下に足したアンカーはキーボードとは別の親にいるので個別に消す。
            foreach (var anchor in _interactorAnchors)
            {
                if (anchor != null) Destroy(anchor);
            }

            _interactorAnchors.Clear();
        }

        /// <summary>キーボードを表示する。初回はここでプレハブを生成する。</summary>
        public void Show()
        {
            if (!enabled) return;

            if (_keyboard == null)
            {
                _keyboard = CreateKeyboard();
                if (_keyboard == null) return;

                // 生成直後は OnEnable 済み(= 表示済み)なので、ここで戻る。
                return;
            }

            ApplyPlacement(_keyboard.transform);
            _keyboard.gameObject.SetActive(true);
        }

        /// <summary>キーボードを隠す。設定画面を閉じるときにも呼ぶ。</summary>
        public void Hide()
        {
            if (_keyboard == null) return;
            _keyboard.gameObject.SetActive(false);
        }

        private void HandleSelect(string _) => Show();

        private void HandleDeselect(string _) => Hide();

        private OVRVirtualKeyboard CreateKeyboard()
        {
            // Instantiate した時点で Awake / OnEnable が走り、キーボードの生成と
            // モデルの読み込みが始まる。位置と入力ソースはその前に決めておく必要があるので、
            // 位置は Instantiate の引数で渡し、残りは Awake 後・初回 Update 前に差し込む。
            GetPlacement(out var position, out var rotation);
            var keyboard = Instantiate(keyboardPrefab, position, rotation);
            keyboard.name = "YummyVerse Virtual Keyboard";

            _textHandler = keyboard.gameObject.AddComponent<TMPVirtualKeyboardTextHandler>();
            _textHandler.InputField = inputField;
            keyboard.TextHandler = _textHandler;

            BindInputSources(keyboard);

            // 入力ソース(_inputSources)は初回 Update で組み立てられるため、
            // ここまでに transform を設定しておけば Custom 配置も反映される。
            keyboard.UseSuggestedLocation(positionMode);
            return keyboard;
        }

        /// <summary>
        /// コントローラー / ハンドをキーボードの入力ソースとして登録する。
        /// Building Block の Virtual Keyboard と同じ配線。
        /// </summary>
        private void BindInputSources(OVRVirtualKeyboard keyboard)
        {
            var cameraRig = FindAnyObjectByType<OVRCameraRig>();
            if (cameraRig != null)
            {
                keyboard.leftControllerRootTransform = cameraRig.leftControllerAnchor;
                keyboard.rightControllerRootTransform = cameraRig.rightControllerAnchor;
                keyboard.leftControllerDirectTransform =
                    CreateInteractorAnchor(cameraRig.leftControllerAnchor, "KeyboardInteractorAnchorLeft");
                keyboard.rightControllerDirectTransform =
                    CreateInteractorAnchor(cameraRig.rightControllerAnchor, "KeyboardInteractorAnchorRight");
            }
            else
            {
                Debug.LogWarning($"{nameof(VirtualKeyboardView)}: OVRCameraRig が見つからないため、コントローラー入力を接続できません。", this);
            }

            // OVRHand.HandType は internal なので、併設の OVRSkeleton から左右を判定する。
            foreach (var hand in FindObjectsByType<OVRHand>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!hand.TryGetComponent<OVRSkeleton>(out var skeleton)) continue;

                switch (skeleton.GetSkeletonType())
                {
                    case OVRSkeleton.SkeletonType.HandLeft:
                    case OVRSkeleton.SkeletonType.XRHandLeft:
                        keyboard.handLeft = hand;
                        break;
                    case OVRSkeleton.SkeletonType.HandRight:
                    case OVRSkeleton.SkeletonType.XRHandRight:
                        keyboard.handRight = hand;
                        break;
                }
            }
        }

        /// <summary>直接タッチ入力で指先として扱う位置。コントローラー先端あたりに置く。</summary>
        private Transform CreateInteractorAnchor(Transform parent, string name)
        {
            if (parent == null) return null;

            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = new Vector3(0f, 0f, 0.062f);
            anchor.localRotation = Quaternion.identity;
            _interactorAnchors.Add(anchor.gameObject);
            return anchor;
        }

        private void ApplyPlacement(Transform keyboardTransform)
        {
            if (positionMode != OVRVirtualKeyboard.KeyboardPosition.Custom) return;

            GetPlacement(out var position, out var rotation);
            keyboardTransform.SetPositionAndRotation(position, rotation);
        }

        private void GetPlacement(out Vector3 position, out Quaternion rotation)
        {
            var anchor = placementAnchor != null ? placementAnchor : transform;

            // 設定パネルは正面(anchor.forward)を向いているので、その手前下に置く。
            // パネルと視線上で重なるとキーボードを押すトリガーがパネル側のレイに拾われてしまう。
            position = anchor.position
                       - anchor.up * verticalOffset
                       - anchor.forward * nearOffset;
            rotation = anchor.rotation * Quaternion.Euler(tiltAngle, 0f, 0f);
        }
    }
}
