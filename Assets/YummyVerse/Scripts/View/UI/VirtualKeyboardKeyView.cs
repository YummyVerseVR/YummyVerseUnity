using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>キーの種類。<see cref="VirtualKeyboardKeyKind.Character"/> 以外は動作が固定。</summary>
    public enum VirtualKeyboardKeyKind
    {
        Character,
        Shift,
        Backspace,
        Space,
        Clear,
        Enter,
    }

    /// <summary>
    /// 仮想キーボードのキー1つ。押されたことを <see cref="VirtualKeyboardPanelView"/> に伝えるだけで、
    /// 入力欄の書き換えは持たない。
    /// </summary>
    /// <remarks>
    /// キーの並びは <c>YummyVerse/UI/Rebuild Virtual Keyboard</c> で生成するが、
    /// 生成後はただの GameObject なので、位置も文字もインスペクタで直せる。
    /// </remarks>
    public sealed class VirtualKeyboardKeyView : MonoBehaviour
    {
        [SerializeField] private VirtualKeyboardKeyKind kind = VirtualKeyboardKeyKind.Character;

        [Tooltip("Shift が無効なときに入る文字。Character 以外では使わない。")]
        [SerializeField] private string character = string.Empty;

        [Tooltip("Shift が有効なときに入る文字。空なら character と同じ扱い。")]
        [SerializeField] private string shiftedCharacter = string.Empty;

        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button button;

        private VirtualKeyboardPanelView _panel;

        public VirtualKeyboardKeyKind Kind => kind;

        public string Character => character;

        public string ShiftedCharacter =>
            string.IsNullOrEmpty(shiftedCharacter) ? character : shiftedCharacter;

        internal void Bind(VirtualKeyboardPanelView panel)
        {
            _panel = panel;
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        internal void Unbind()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            _panel = null;
        }

        /// <summary>Shift の状態に合わせて表示を切り替える。文字キー以外は固定ラベルなので触らない。</summary>
        internal void ApplyShift(bool isShifted)
        {
            if (kind != VirtualKeyboardKeyKind.Character || label == null) return;
            label.text = isShifted ? ShiftedCharacter : character;
        }

        private void HandleClick()
        {
            if (_panel != null) _panel.HandleKeyPressed(this);
        }
    }
}
