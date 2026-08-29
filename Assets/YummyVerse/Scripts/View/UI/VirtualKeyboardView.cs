using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using YummyVerse.Scripts.Presentation;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// 入力欄にフォーカスが入っているあいだだけ仮想キーボードを出す。
    /// </summary>
    /// <remarks>
    /// Quest 単体で使うシステムキーボード(TouchScreenKeyboard)は PC ビルドでは一切使えないため、
    /// PCVR でも動く自前のキーボードを設定ダイアログのプレハブに組み込んでいる。
    ///
    /// Meta の <c>OVRVirtualKeyboard</c> は使わない。あれは Unity のワールド座標を
    /// そのままトラッキング空間の姿勢としてランタイムに渡す作り (OVRVirtualKeyboard.ComputeLocation) で、
    /// OVRCameraRig が原点にある前提になっている。このシーンは FirstPersonLocomotor が
    /// Camera Rig 自体を動かすので、プレイヤーが移動した分だけキーボードがズレる。
    /// 同じ SDK でも OVROverlay や OVRSpatialAnchor は ToTrackingSpacePose() を通しているのに、
    /// キーボードだけ変換がなく、こちら側では直しようがない。
    /// </remarks>
    public sealed class VirtualKeyboardView : MonoBehaviour, IVirtualKeyboard
    {
        /// <summary>設定ダイアログのプレハブに組み込んであるキーボード。</summary>
        [SerializeField] private VirtualKeyboardPanelView keyboard;

        /// <summary>キーボードを出す対象の入力欄。</summary>
        [SerializeField] private TMP_InputField inputField;

        private Coroutine _pendingHide;

        /// <summary>
        /// キーボードを閉じたとき、そのときの入力欄の中身を渡す。編集の確定はここ一本。
        /// </summary>
        public event Action<string> EditingFinished;

        /// <summary>キーボードが開いている=まだ打鍵の途中かどうか。</summary>
        public bool IsEditing => keyboard != null && keyboard.gameObject.activeSelf;

        private void Awake()
        {
            if (inputField == null || keyboard == null)
            {
                Debug.LogWarning($"{nameof(VirtualKeyboardView)}: inputField / keyboard が未設定です。", this);
                enabled = false;
                return;
            }

            // 入力手段を仮想キーボードに一本化する。
            // (Android 実機ではこれを切らないと OS のシステムキーボードも同時に出る)
            inputField.shouldHideSoftKeyboard = true;

            // キーを押すたびにフォーカスを入力欄へ戻す(HandleDeselect)ので、
            // 戻すたびに全選択されると打った文字が消えたように見える。
            inputField.onFocusSelectAll = false;

            keyboard.Initialize(inputField);
            keyboard.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (inputField == null || keyboard == null) return;
            inputField.onSelect.AddListener(HandleSelect);
            inputField.onDeselect.AddListener(HandleDeselect);
            keyboard.Submitted += HandleSubmitted;
        }

        private void OnDisable()
        {
            if (inputField != null)
            {
                inputField.onSelect.RemoveListener(HandleSelect);
                inputField.onDeselect.RemoveListener(HandleDeselect);
            }

            if (keyboard != null) keyboard.Submitted -= HandleSubmitted;

            Hide();
        }

        /// <summary>キーボードを表示する。</summary>
        public void Show()
        {
            if (!enabled || keyboard == null) return;

            CancelPendingHide();
            keyboard.gameObject.SetActive(true);
        }

        void IVirtualKeyboard.Close() => Hide();

        /// <summary>キーボードを隠す。設定画面を閉じるときにも呼ぶ。</summary>
        public void Hide()
        {
            CancelPendingHide();
            if (keyboard == null) return;

            var wasEditing = keyboard.gameObject.activeSelf;
            keyboard.gameObject.SetActive(false);

            if (wasEditing && inputField != null) EditingFinished?.Invoke(inputField.text);
        }

        private void HandleSelect(string _) => Show();

        /// <remarks>
        /// キーも Selectable なので、押した瞬間に入力欄からフォーカスが外れて onDeselect が飛ぶ。
        /// そこで即座には閉じず、1フレーム待ってから行き先を見る。フォーカスがキーボードの中へ
        /// 移っただけなら入力欄に戻して開いたままにし、それ以外なら閉じる。
        /// </remarks>
        private void HandleDeselect(string _)
        {
            CancelPendingHide();

            if (!isActiveAndEnabled)
            {
                Hide();
                return;
            }

            _pendingHide = StartCoroutine(HideUnlessFocusMovedIntoKeyboard());
        }

        private IEnumerator HideUnlessFocusMovedIntoKeyboard()
        {
            yield return null;
            _pendingHide = null;

            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;

            if (keyboard != null && keyboard.Contains(selected))
            {
                inputField.ActivateInputField();
                yield break;
            }

            Hide();
        }

        private void HandleSubmitted() => Hide();

        private void CancelPendingHide()
        {
            if (_pendingHide == null) return;
            StopCoroutine(_pendingHide);
            _pendingHide = null;
        }
    }
}
