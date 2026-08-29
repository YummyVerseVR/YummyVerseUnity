using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.View.UI;

namespace YummyVerse.Editor.Tests
{
    /// <summary>
    /// 設定ダイアログのプレハブに入っている仮想キーボードを、実物のまま叩いて確かめる。
    /// キーの配線 (<c>YummyVerse/UI/Rebuild Virtual Keyboard</c> の生成結果) と
    /// 打鍵ロジックの両方が対象。
    /// </summary>
    public class VirtualKeyboardPanelViewTests
    {
        private const string PrefabPath = "Assets/YummyVerse/Prefabs/Restaurant/UI/YummyConfigUI.prefab";
        private const string KeyRoot = "CanvasRoot/UIBackplate/";

        private GameObject _instance;
        private VirtualKeyboardPanelView _panel;
        private Transform _keyboard;
        private TMP_InputField _inputField;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, PrefabPath + " が読めない");

            _instance = Object.Instantiate(prefab);
            _keyboard = _instance.transform.Find("VirtualKeyboard");
            Assert.That(_keyboard, Is.Not.Null, "プレハブに VirtualKeyboard がない");

            _panel = _keyboard.GetComponent<VirtualKeyboardPanelView>();
            _inputField = _instance.transform
                .Find("CanvasRoot/UIBackplate/TextInputField/TextField")
                .GetComponent<TMP_InputField>();

            // 編集モードでは Awake が走らないので、実行時と同じ入り口を自分で呼ぶ。
            _panel.Initialize(_inputField);
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
            var view = _instance.transform.Find("View").GetComponent<VirtualKeyboardView>();
            Assert.That(view, Is.Not.Null, "View に VirtualKeyboardView がない");
            Assert.That(_panel.InputField, Is.EqualTo(_inputField));
            Assert.That(_keyboard.gameObject.activeSelf, Is.False, "キーボードは閉じた状態で保存されているはず");
        }

        [Test]
        public void CharacterKeys_AppendToTheInputField()
        {
            PressAll("Row2/Key_p", "Row2/Key_o", "Row3/Key_Colon", "Row4/Key_Slash", "Row4/Key_Slash");
            Assert.That(_inputField.text, Is.EqualTo("po://"));
        }

        [Test]
        public void Shift_SwitchesBothTheLabelAndTheInsertedCharacter()
        {
            var label = _keyboard.Find(KeyRoot + "Row3/Key_a/Label").GetComponent<TextMeshProUGUI>();
            Assert.That(label.text, Is.EqualTo("a"));

            Press("Row5/Key_Shift");
            Assert.That(label.text, Is.EqualTo("A"), "Shift でラベルが切り替わっていない");

            PressAll("Row3/Key_a", "Row1/Key_1");
            Assert.That(_inputField.text, Is.EqualTo("A!"));

            Press("Row5/Key_Shift");
            Assert.That(label.text, Is.EqualTo("a"));
            Press("Row3/Key_a");
            Assert.That(_inputField.text, Is.EqualTo("A!a"));
        }

        [Test]
        public void Backspace_RemovesTheLastCharacter_AndIsSafeWhenEmpty()
        {
            Press("Row5/Key_Backspace");
            Assert.That(_inputField.text, Is.Empty);

            PressAll("Row3/Key_a", "Row3/Key_s");
            Press("Row5/Key_Backspace");
            Assert.That(_inputField.text, Is.EqualTo("a"));
        }

        [Test]
        public void SpaceAndClear_Work()
        {
            PressAll("Row3/Key_a", "Row5/Key_Space", "Row3/Key_s");
            Assert.That(_inputField.text, Is.EqualTo("a s"));

            Press("Row5/Key_Clear");
            Assert.That(_inputField.text, Is.Empty);
        }

        [Test]
        public void Enter_RaisesOnEndEditAndSubmitted()
        {
            var endEdit = 0;
            var submitted = 0;
            _inputField.onEndEdit.AddListener(_ => endEdit++);
            _panel.Submitted += () => submitted++;

            Press("Row3/Key_a");
            Assert.That(endEdit, Is.Zero, "文字入力だけで確定してはいけない");

            Press("Row5/Key_Enter");
            Assert.That(endEdit, Is.EqualTo(1));
            Assert.That(submitted, Is.EqualTo(1));
            Assert.That(_inputField.text, Is.EqualTo("a"), "確定で中身が変わってはいけない");
        }

        [Test]
        public void EveryKeyLabel_ExistsInTheFont()
        {
            // フォントに無い文字は実行時に豆腐になるだけで気づけないので、ここで落とす。
            foreach (var label in _keyboard.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                foreach (var c in label.text)
                {
                    Assert.That(label.font.HasCharacter(c), Is.True,
                        $"{label.font.name} に '{c}' (U+{(int)c:X4}) が無い: {label.transform.parent.name}");
                }
            }
        }

        private void Press(string path)
        {
            var key = _keyboard.Find(KeyRoot + path);
            Assert.That(key, Is.Not.Null, path + " が見つからない");
            key.GetComponent<Button>().onClick.Invoke();
        }

        private void PressAll(params string[] paths)
        {
            foreach (var path in paths) Press(path);
        }
    }
}
