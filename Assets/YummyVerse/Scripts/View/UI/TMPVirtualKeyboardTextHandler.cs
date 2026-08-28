using System;
using TMPro;
using UnityEngine;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// OVRVirtualKeyboard を TextMeshPro の入力欄に繋ぐためのテキストハンドラ。
    /// </summary>
    /// <remarks>
    /// SDK 同梱の OVRVirtualKeyboardInputFieldTextHandler は uGUI の InputField 専用で、
    /// TMP_InputField を扱えない。プロジェクトの入力欄は Meta QDS の TextInputField
    /// (TMP_InputField) なので、同等の実装を TMP 向けに用意する。
    /// </remarks>
    public sealed class TMPVirtualKeyboardTextHandler : OVRVirtualKeyboard.AbstractTextHandler
    {
        [SerializeField] private TMP_InputField inputField;

        private bool _isBound;

        /// <summary>
        /// 接続先の入力欄。実行時に差し替えても購読が付け替わる。
        /// </summary>
        public TMP_InputField InputField
        {
            get => inputField;
            set
            {
                if (value == inputField) return;

                Unbind();
                inputField = value;
                Bind();

                OnTextChanged?.Invoke(Text);
            }
        }

        public override Action<string> OnTextChanged { get; set; }

        public override string Text => inputField != null ? inputField.text : string.Empty;

        // 単行入力なので Enter は改行ではなく確定として扱われる。
        public override bool SubmitOnEnter =>
            inputField != null && inputField.lineType != TMP_InputField.LineType.MultiLineNewline;

        public override bool IsFocused => inputField != null && inputField.isFocused;

        public override void Submit()
        {
            if (inputField == null) return;
            inputField.onEndEdit.Invoke(inputField.text);
        }

        public override void AppendText(string s)
        {
            if (inputField == null) return;
            inputField.text += s;
        }

        public override void ApplyBackspace()
        {
            if (inputField == null || string.IsNullOrEmpty(inputField.text)) return;
            inputField.text = Text.Substring(0, Text.Length - 1);
        }

        public override void MoveTextEnd()
        {
            if (inputField == null) return;
            inputField.MoveTextEnd(false);
        }

        private void Start()
        {
            // インスペクタで設定された場合はここが初回の購読になる。
            // 実行時に InputField を代入済みなら二重購読しない。
            Bind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_isBound || inputField == null) return;
            inputField.onValueChanged.AddListener(ProxyOnValueChanged);
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound) return;
            if (inputField != null)
            {
                inputField.onValueChanged.RemoveListener(ProxyOnValueChanged);
            }
            _isBound = false;
        }

        private void ProxyOnValueChanged(string value)
        {
            OnTextChanged?.Invoke(value);
        }
    }
}
