using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using YummyVerse.Scripts.View.UI;

namespace YummyVerse.Editor.Tests
{
    /// <summary>
    /// 設定ダイアログのプレハブに入っている仮想キーボードを、実物のまま叩いて確かめる。
    /// キーの配線 (<c>YummyVerse/UI/Rebuild Virtual Keyboard</c> の生成結果) と
    /// 打鍵ロジックの両方が対象。
    /// </summary>
    /// <remarks>
    /// キーは名前ではなく持っているデータ (種類と文字) で引く。名前で引くと、並びを
    /// 変えたときにテストだけ黙って別のキーを叩くようになる。
    /// </remarks>
    public class VirtualKeyboardPanelViewTests
    {
        private const string PrefabPath = "Assets/YummyVerse/Prefabs/Restaurant/UI/YummyConfigUI.prefab";

        private GameObject _instance;
        private VirtualKeyboardPanelView _panel;
        private VirtualKeyboardKeyView[] _keys;
        private TMP_InputField _inputField;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, PrefabPath + " が読めない");

            _instance = Object.Instantiate(prefab);

            _panel = _instance.GetComponentInChildren<VirtualKeyboardPanelView>(true);
            Assert.That(_panel, Is.Not.Null, "プレハブに VirtualKeyboardPanelView がない");

            var inputFields = _instance.GetComponentsInChildren<TMP_InputField>(true);
            Assert.That(inputFields.Length, Is.EqualTo(1), "入力欄が1つだけである前提が崩れている");
            _inputField = inputFields[0];

            // 編集モードでは Awake が走らないので、実行時と同じ入り口を自分で呼ぶ。
            _panel.Initialize(_inputField);
            _keys = _panel.GetComponentsInChildren<VirtualKeyboardKeyView>(true);
            _inputField.text = string.Empty;
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
        }

        [Test]
        public void Prefab_WiresKeyboardToTheInputField()
        {
            var view = _instance.GetComponentInChildren<VirtualKeyboardView>(true);
            Assert.That(view, Is.Not.Null, "VirtualKeyboardView がない");
            Assert.That(_panel.InputField, Is.EqualTo(_inputField));
            Assert.That(_panel.gameObject.activeSelf, Is.False, "キーボードは閉じた状態で保存されているはず");
            Assert.That(_keys, Is.Not.Empty);
        }

        [Test]
        public void EveryKeyIsDrivenByPointerDown_NotByClick()
        {
            // click は「押した相手と離した相手が同じ」ことを要求し、押している間に
            // 少し動いただけで捨てられる。VR では取りこぼすので使ってはいけない。
            foreach (var key in _keys)
            {
                Assert.That(key, Is.InstanceOf<IPointerDownHandler>(), key.name);
                Assert.That(key.GetComponent<UnityEngine.UI.Button>(), Is.Null,
                    key.name + " に Button が付いている (onClick 経由になる恐れ)");
            }
        }

        [Test]
        public void CharacterKeys_AppendToTheInputField()
        {
            PressAll("p", "o", ":", "/", "/");
            Assert.That(_inputField.text, Is.EqualTo("po://"));
        }

        [Test]
        public void Shift_SwitchesBothTheLabelAndTheInsertedCharacter()
        {
            var a = Key("a");
            var label = a.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.That(label.text, Is.EqualTo("a"));

            Press(Key(VirtualKeyboardKeyKind.Shift));
            Assert.That(label.text, Is.EqualTo("A"), "Shift でラベルが切り替わっていない");

            PressAll("a", "1");
            Assert.That(_inputField.text, Is.EqualTo("A!"));

            Press(Key(VirtualKeyboardKeyKind.Shift));
            Assert.That(label.text, Is.EqualTo("a"));
            PressAll("a");
            Assert.That(_inputField.text, Is.EqualTo("A!a"));
        }

        [Test]
        public void Backspace_RemovesTheLastCharacter_AndIsSafeWhenEmpty()
        {
            Press(Key(VirtualKeyboardKeyKind.Backspace));
            Assert.That(_inputField.text, Is.Empty);

            PressAll("a", "s");
            Press(Key(VirtualKeyboardKeyKind.Backspace));
            Assert.That(_inputField.text, Is.EqualTo("a"));
        }

        [Test]
        public void SpaceAndClear_Work()
        {
            PressAll("a");
            Press(Key(VirtualKeyboardKeyKind.Space));
            PressAll("s");
            Assert.That(_inputField.text, Is.EqualTo("a s"));

            Press(Key(VirtualKeyboardKeyKind.Clear));
            Assert.That(_inputField.text, Is.Empty);
        }

        [Test]
        public void Enter_RaisesOnEndEditAndSubmitted()
        {
            var endEdit = 0;
            var submitted = 0;
            _inputField.onEndEdit.AddListener(_ => endEdit++);
            _panel.Submitted += () => submitted++;

            PressAll("a");
            Assert.That(endEdit, Is.Zero, "文字入力だけで確定してはいけない");

            Press(Key(VirtualKeyboardKeyKind.Enter));
            Assert.That(endEdit, Is.EqualTo(1));
            Assert.That(submitted, Is.EqualTo(1));
            Assert.That(_inputField.text, Is.EqualTo("a"), "確定で中身が変わってはいけない");
        }

        [Test]
        public void EveryKeyLabel_ExistsInTheFont()
        {
            // フォントに無い文字は実行時に豆腐になるだけで気づけないので、ここで落とす。
            foreach (var label in _panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                foreach (var c in label.text)
                {
                    Assert.That(label.font.HasCharacter(c), Is.True,
                        $"{label.font.name} に '{c}' (U+{(int)c:X4}) が無い: {label.transform.parent.name}");
                }
            }
        }

        [Test]
        public void UrlCharacters_AreAllReachable()
        {
            // API エンドポイントの URL が打てなければキーボードの意味がない。
            const string needed = "abcdefghijklmnopqrstuvwxyz0123456789:/.-_?=&%";
            foreach (var c in needed)
            {
                var text = c.ToString();
                Assert.That(
                    _keys.Any(k => k.Kind == VirtualKeyboardKeyKind.Character
                                   && (k.Character == text || k.ShiftedCharacter == text)),
                    Is.True, $"'{c}' が打てない");
            }
        }

        private VirtualKeyboardKeyView Key(string character) =>
            Single(_keys.Where(k => k.Kind == VirtualKeyboardKeyKind.Character && k.Character == character),
                $"文字キー '{character}'");

        private VirtualKeyboardKeyView Key(VirtualKeyboardKeyKind kind) =>
            Single(_keys.Where(k => k.Kind == kind), kind.ToString());

        private static VirtualKeyboardKeyView Single(
            System.Collections.Generic.IEnumerable<VirtualKeyboardKeyView> found, string what)
        {
            var list = found.ToList();
            Assert.That(list.Count, Is.EqualTo(1), $"{what} が {list.Count} 個ある");
            return list[0];
        }

        private static void Press(VirtualKeyboardKeyView key) =>
            key.OnPointerDown(new PointerEventData(EventSystem.current));

        private void PressAll(params string[] characters)
        {
            foreach (var c in characters) Press(Key(c));
        }
    }
}
