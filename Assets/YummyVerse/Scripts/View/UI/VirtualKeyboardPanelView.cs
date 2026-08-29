using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// 仮想キーボード本体。子の <see cref="VirtualKeyboardKeyView"/> を集め、
    /// 押されたキーを対象の <see cref="TMP_InputField"/> に反映する。
    /// </summary>
    /// <remarks>
    /// 入力は常に末尾へ足し、キャレットも末尾に置く。対象が API エンドポイントの URL 一本で、
    /// VR ではキャレットを狙って置く操作の方が難しいため。
    ///
    /// 描画も当たり判定もただの uGUI なので、Meta のバーチャルキーボードと違い
    /// Scene ビューで見えるし、設定ダイアログとの相対位置は親子関係だけで決まる。
    /// </remarks>
    public sealed class VirtualKeyboardPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;

        [Tooltip("Shift が入っているあいだ色を変えるキーの背景。")]
        [SerializeField] private Graphic shiftKeyGraphic;

        [SerializeField] private Color shiftOffColor = Color.white;
        [SerializeField] private Color shiftOnColor = new(0.42f, 0.72f, 1f, 1f);

        private readonly List<VirtualKeyboardKeyView> _keys = new();
        private bool _isShifted;

        /// <summary>Enter が押されたとき。</summary>
        public event Action Submitted;

        public TMP_InputField InputField => inputField;

        /// <summary>指定の GameObject がこのキーボードの一部かどうか。フォーカス制御に使う。</summary>
        public bool Contains(GameObject candidate) =>
            candidate != null && candidate.transform.IsChildOf(transform);

        /// <summary>
        /// 入力先を差して、子のキーを繋ぎ直す。
        /// </summary>
        /// <remarks>
        /// キーボードは閉じている=非アクティブなので Awake が走らない。開くより先に
        /// 繋いでおけるよう外から呼べるようにしてある。何度呼んでも二重購読しない。
        /// </remarks>
        public void Initialize(TMP_InputField target)
        {
            inputField = target;

            UnbindKeys();
            GetComponentsInChildren(true, _keys);
            foreach (var key in _keys) key.Bind(this);

            _isShifted = false;
            ApplyShift();
        }

        private void Awake() => Initialize(inputField);

        private void OnEnable()
        {
            // 開くたびに Shift は解除しておく。前回の状態が残っていると押し間違いになる。
            _isShifted = false;
            ApplyShift();
        }

        private void OnDestroy() => UnbindKeys();

        private void UnbindKeys()
        {
            foreach (var key in _keys) key.Unbind();
            _keys.Clear();
        }

        internal void HandleKeyPressed(VirtualKeyboardKeyView key)
        {
            if (inputField == null || key == null) return;

            switch (key.Kind)
            {
                case VirtualKeyboardKeyKind.Character:
                    Insert(_isShifted ? key.ShiftedCharacter : key.Character);
                    break;
                case VirtualKeyboardKeyKind.Space:
                    Insert(" ");
                    break;
                case VirtualKeyboardKeyKind.Backspace:
                    Backspace();
                    break;
                case VirtualKeyboardKeyKind.Clear:
                    SetText(string.Empty);
                    break;
                case VirtualKeyboardKeyKind.Shift:
                    _isShifted = !_isShifted;
                    ApplyShift();
                    break;
                case VirtualKeyboardKeyKind.Enter:
                    // 確定は入力欄の onEndEdit に流す(ConfigUIPresenter がここを購読している)。
                    inputField.onEndEdit.Invoke(inputField.text);
                    Submitted?.Invoke();
                    break;
            }
        }

        private void Insert(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            SetText(inputField.text + value);
        }

        private void Backspace()
        {
            var text = inputField.text;
            if (string.IsNullOrEmpty(text)) return;
            SetText(text.Substring(0, text.Length - 1));
        }

        private void SetText(string value)
        {
            // onValueChanged 経由で ViewModel まで届くよう、SetTextWithoutNotify は使わない。
            inputField.text = value;
            inputField.caretPosition = value.Length;
            inputField.selectionAnchorPosition = value.Length;
            inputField.selectionFocusPosition = value.Length;
        }

        private void ApplyShift()
        {
            foreach (var key in _keys) key.ApplyShift(_isShifted);
            if (shiftKeyGraphic != null) shiftKeyGraphic.color = _isShifted ? shiftOnColor : shiftOffColor;
        }
    }
}
