using System.Collections;
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

        /// <summary>キーボードを固定する基準。設定ダイアログのルート(パネル中心)を指す。</summary>
        [SerializeField] private Transform panelAnchor;

        /// <summary>
        /// Custom = 下の3項目で自分で決める(既定)。
        /// Near / Far はランタイム任せの位置で、こちらの設定は一切効かない。
        /// </summary>
        [Header("Placement")]
        [SerializeField]
        private OVRVirtualKeyboard.KeyboardPosition positionMode = OVRVirtualKeyboard.KeyboardPosition.Custom;

        [Tooltip("設定ダイアログ中心から見た表示位置[m]。x=右, y=上(負で下), z=奥(負で手前)。パネルは縦0.39mなので、下端は y=-0.195。")]
        [SerializeField] private Vector3 positionOffset = new(0f, -0.3f, -0.05f);

        [Tooltip("パネル面からさらに手前に倒す角度[deg]。0 でパネルと同じ向き、90 で水平。")]
        [SerializeField] private float tiltAngle = 30f;

        [Tooltip("キーボードの大きさ。1 で幅1.0m×高さ0.4mの実寸になる。パネル幅が0.57mなので 0.4 前後が収まりが良い。")]
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
                return;
            }

            // ランタイム側の生成に一度でも失敗していると、GameObject は生きたまま
            // 中身(キーボード本体とモデル)だけ破棄された状態になる。SetActive(true) は
            // 既に active だと何も起きないので、必ず OnEnable を通してやり直させる。
            _keyboard.gameObject.SetActive(false);
            _keyboard.gameObject.SetActive(true);

            // 位置はランタイム側の空間に固定されるので、開き直すたびに置き直す。
            StartCoroutine(ApplyPlacementNextFrame(_keyboard));
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
            // Instantiate した時点で Awake / OnEnable が走り、ランタイム側のキーボード生成と
            // モデルの読み込みが始まる。位置と向きだけは原点に出さないためここで渡すが、
            // 大きさはプレハブのまま(等倍)にしておく(理由は下のコルーチン)。
            GetPose(out var position, out var rotation);
            var keyboard = Instantiate(keyboardPrefab, position, rotation);
            keyboard.name = "YummyVerse Virtual Keyboard";

            _textHandler = keyboard.gameObject.AddComponent<TMPVirtualKeyboardTextHandler>();
            _textHandler.InputField = inputField;
            keyboard.TextHandler = _textHandler;

            BindInputSources(keyboard);

            StartCoroutine(ApplyPlacementNextFrame(keyboard));
            return keyboard;
        }

        /// <summary>
        /// ランタイム側のキーボード空間ができてから位置・大きさを反映する。
        /// </summary>
        /// <remarks>
        /// キーボード空間は表示後の初回 Update(SyncKeyboardLocation → GetKeyboardSpace)で、
        /// そのときの Transform をそのまま姿勢として作られる。生成と同じフレームに
        /// Transform を動かしてしまうと、その姿勢で空間生成が走り、ランタイムが受け付けないと
        /// SDK 側が DestroyKeyboard() まで実行してキーボードごと消える
        /// (OVRVirtualKeyboard.GetKeyboardSpace の失敗時処理)。
        /// この場合 GameObject だけが残るため、外からは「キーボードが出ない」ようにしか見えない。
        /// </remarks>
        private IEnumerator ApplyPlacementNextFrame(OVRVirtualKeyboard keyboard)
        {
            yield return null;

            if (keyboard == null || !keyboard.gameObject.activeInHierarchy) yield break;

            ApplyPlacement(keyboard.transform);
            keyboard.UseSuggestedLocation(positionMode);
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
        /// 頭ではなく設定ダイアログを基準にしているのは、ダイアログ自体が開くときに
        /// カメラ前へ配置されるので、そこからの相対位置なら見えている物との関係が
        /// そのまま決まるため。頭基準だと配置がトラッキング空間の状態に左右される。
        ///
        /// Near / Far モードを使わないのは、どちらもランタイムが位置を決めるモードで、
        /// こちらの Transform が毎フレーム上書きされてしまうため(SDK ドキュメント:
        /// "If set to Far or Near, the keyboard position is runtime controlled,
        /// so the Transform component will be locked")。
        ///
        /// 大きさも Transform で決まる。localScale の最大成分がそのまま倍率として
        /// ランタイムに渡り(ComputeLocation → MaxElement)、1 のときキーボードの
        /// 実寸は幅1.0m×高さ0.4mになる(OVRVirtualKeyboard.OnDrawGizmos 参照)。
        ///
        /// 呼ぶのは表示するときの1回だけ。毎フレーム書き込むと、ランタイム側の姿勢を
        /// Transform に書き戻す SyncKeyboardLocation と取り合いになって震える。
        /// </remarks>
        private void ApplyPlacement(Transform keyboardTransform)
        {
            if (positionMode != OVRVirtualKeyboard.KeyboardPosition.Custom) return;
            if (panelAnchor == null) return;

            GetPose(out var position, out var rotation);
            keyboardTransform.SetPositionAndRotation(position, rotation);
            keyboardTransform.localScale = Vector3.one * scale;
        }

        /// <summary>設定ダイアログ基準のキーボードの姿勢。</summary>
        private void GetPose(out Vector3 position, out Quaternion rotation)
        {
            if (panelAnchor == null)
            {
                Debug.LogWarning($"{nameof(VirtualKeyboardView)}: panelAnchor が未設定です。", this);
                position = transform.position;
                rotation = transform.rotation;
                return;
            }

            // パネルのスケールを持ち込まないよう、position + rotation * offset で組む。
            position = panelAnchor.position + panelAnchor.rotation * positionOffset;
            rotation = panelAnchor.rotation * Quaternion.Euler(tiltAngle, 0f, 0f);
        }
    }
}
