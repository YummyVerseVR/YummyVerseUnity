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

        /// <summary>Custom 配置の基準にする頭のカメラ。未設定なら Camera.main。</summary>
        [SerializeField] private Camera targetCamera;

        /// <summary>
        /// Custom = 下の3項目で自分で決める(既定)。
        /// Near / Far はランタイム任せの位置で、こちらの設定は一切効かない。
        /// </summary>
        [Header("Placement")]
        [SerializeField]
        private OVRVirtualKeyboard.KeyboardPosition positionMode = OVRVirtualKeyboard.KeyboardPosition.Custom;

        [Tooltip("頭から見た表示位置[m]。x=右, y=上(負で下), z=前。SDK の Near 相当は (0, -0.4, 0.4)、Far 相当は (0, -0.5, 1)。")]
        [SerializeField] private Vector3 positionOffset = new(0f, -0.4f, 0.4f);

        [Tooltip("手前に倒す角度[deg]。0 で垂直、90 で水平。SDK の Near 相当は 65、Far 相当は 0。")]
        [SerializeField] private float tiltAngle = 65f;

        [Tooltip("キーボードの大きさ。1 で幅1.0m×高さ0.4mの実寸になる。SDK の Near 相当は 0.4、Far 相当は 1。")]
        [SerializeField] private float scale = 0.4f;

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

            // キーボードの位置はランタイム側の空間に固定されるので、開き直すたびに
            // 置き直さないと、前に開いた場所(移動前の足元など)に取り残される。
            _keyboard.UseSuggestedLocation(positionMode);
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
            // モデルの読み込みが始まる。ランタイム上のキーボードの位置と大きさは
            // 初回 Update の SyncKeyboardLocation で Transform から決まるので、
            // 入力ソースともども、そこまでに設定しておく。
            var keyboard = Instantiate(keyboardPrefab);
            keyboard.name = "YummyVerse Virtual Keyboard";
            ApplyPlacement(keyboard.transform);

            _textHandler = keyboard.gameObject.AddComponent<TMPVirtualKeyboardTextHandler>();
            _textHandler.InputField = inputField;
            keyboard.TextHandler = _textHandler;

            BindInputSources(keyboard);

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

        /// <summary>
        /// Custom のときの位置・向き・大きさを Transform に書き込む。
        /// </summary>
        /// <remarks>
        /// 既定値は SDK が Near(直接タッチ向けの手元配置)の目安として持っている
        /// 値に合わせてある(OVRVirtualKeyboard.OnDrawGizmos 参照)。
        /// Near / Far モードを使わないのは、どちらもランタイムが位置を決めるモードで、
        /// こちらの Transform が毎フレーム上書きされてしまうため。
        ///
        /// 大きさも Transform で決まる。localScale の最大成分がそのまま倍率として
        /// ランタイムに渡り(ComputeLocation → MaxElement)、1 のときキーボードの
        /// 実寸は幅1.0m×高さ0.4mになる。手元に置くなら 0.4 前後まで小さくする。
        ///
        /// また Transform はワールド座標のままトラッキング空間の座標として渡される。
        /// このシーンは OVRCameraRig が原点・無回転・等倍なので両者は一致しているが、
        /// リグを動かす作りに変えるとここがずれる。高さも固定値ではなく
        /// 実行時の頭の高さからの相対で出しているので、原点が FloorLevel でも問題ない。
        ///
        /// 呼ぶのは表示するときの1回だけ。毎フレーム書き込むと、ランタイム側の姿勢を
        /// Transform に書き戻す SyncKeyboardLocation と取り合いになって震える。
        /// </remarks>
        private void ApplyPlacement(Transform keyboardTransform)
        {
            if (positionMode != OVRVirtualKeyboard.KeyboardPosition.Custom) return;

            var camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null) return;

            // 頭の向きのうち水平成分だけを使う。見上げ / 見下ろしで位置が動かないようにするため。
            var head = camera.transform;
            var yaw = Quaternion.Euler(0f, head.eulerAngles.y, 0f);

            keyboardTransform.SetPositionAndRotation(
                head.position + yaw * positionOffset,
                yaw * Quaternion.Euler(tiltAngle, 0f, 0f));
            keyboardTransform.localScale = Vector3.one * scale;
        }
    }
}
