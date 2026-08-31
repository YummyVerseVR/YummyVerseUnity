using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// 反応するのは click ではなく pointer down。uGUI の click は「押した相手と離した相手が
    /// 同じ」ことを要求し (PointableCanvasModule.ProcessPress)、さらに押している間に
    /// pixelDragThreshold を超えて動くと click 自体が捨てられる (同 ProcessDrag →
    /// ClearPointerSelection)。canvas は 2000px/m なので、レイでもポークでも手ブレで
    /// 簡単に閾値を超え、「音は鳴るのに文字が入らない」状態になる。押した時点で確定させる。
    /// </remarks>
    public sealed class VirtualKeyboardKeyView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private VirtualKeyboardKeyKind kind = VirtualKeyboardKeyKind.Character;

        [Tooltip("Shift が無効なときに入る文字。Character 以外では使わない。")]
        [SerializeField] private string character = string.Empty;

        [Tooltip("Shift が有効なときに入る文字。空なら character と同じ扱い。")]
        [SerializeField] private string shiftedCharacter = string.Empty;

        [SerializeField] private TextMeshProUGUI label;

        [Tooltip("押下時の色替え用。入力の受け取りには使わない。")]
        [SerializeField] private Selectable selectable;

        private VirtualKeyboardPanelView _panel;

        public VirtualKeyboardKeyKind Kind => kind;

        public string Character => character;

        public string ShiftedCharacter =>
            string.IsNullOrEmpty(shiftedCharacter) ? character : shiftedCharacter;

        internal void Bind(VirtualKeyboardPanelView panel) => _panel = panel;

        internal void Unbind() => _panel = null;

        /// <summary>Shift の状態に合わせて表示を切り替える。文字キー以外は固定ラベルなので触らない。</summary>
        internal void ApplyShift(bool isShifted)
        {
            if (kind != VirtualKeyboardKeyKind.Character || label == null) return;
            label.text = isShifted ? ShiftedCharacter : character;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_panel == null) return;
            if (selectable != null && !selectable.IsInteractable()) return;
            _panel.HandleKeyPressed(this);
        }
    }
}
