using System.Collections.Generic;
using Oculus.Interaction;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YummyVerse.Scripts.View.UI;

namespace YummyVerse.Editor
{
    /// <summary>
    /// 設定ダイアログのプレハブの中に、仮想キーボードのキーを一括生成する。
    /// </summary>
    /// <remarks>
    /// キーは50個近くあり手で並べるのは現実的でないのでここで作るが、生成後はただの
    /// GameObject なので、位置も文字も色もインスペクタで直せる。並びを変えたくなったら
    /// <see cref="Rows"/> を書き換えてメニューから作り直す(既存の VirtualKeyboard は捨てられる)。
    ///
    /// 土台は Interaction SDK の EmptyUIBackplateWithCanvas。設定ダイアログ本体と同じ
    /// プレハブなので、PokeInteractable / RayInteractable / PointableCanvas 一式が
    /// 最初から配線済みで、poke も ray も追加の作業なしで効く。
    /// </remarks>
    public static class VirtualKeyboardBuilder
    {
        private const string PrefabPath = "Assets/YummyVerse/Prefabs/Restaurant/UI/YummyConfigUI.prefab";
        private const string BackplatePrefabGuid = "6b196de96e7d16b4297c1980179ae439"; // EmptyUIBackplateWithCanvas
        private const string FontPath = "Assets/YummyVerse/Misc/MPLUS1p-Regular SDF.asset";
        private const string InputFieldPath = "CanvasRoot/UIBackplate/TextInputField/TextField";
        private const string KeyboardName = "VirtualKeyboard";

        // 設定ダイアログ中心から見た配置。パネルは縦0.39mなので下端は y=-0.195。
        private static readonly Vector3 LocalPosition = new(0f, -0.3f, -0.05f);
        private static readonly Vector3 LocalEulerAngles = new(30f, 0f, 0f);

        // Canvas は 1 unit = 0.0005m。1000x400 で実寸 0.5m x 0.2m になる。
        private static readonly Vector2 CanvasSize = new(1000f, 400f);

        private const float KeyFontSize = 30f;
        private const float RowSpacing = 8f;
        private const float KeySpacing = 8f;
        private const int BackplatePadding = 14;
        private const float KeyBorderRadius = 12f;

        // テーマ (UIThemeQuest_Dark_JP) と同じ値。キーボードは閉じている=非アクティブなので
        // UIThemeManager.Start の GetComponentsInChildren に拾われない。色はここで確定させる。
        private static readonly Color KeyColor = new(1f, 1f, 1f, 0.902f);
        private static readonly Color KeyLabelColor = new(0.153f, 0.153f, 0.153f, 1f);
        private static readonly Color ShiftOnColor = new(0.42f, 0.72f, 1f, 0.902f);

        private readonly struct Key
        {
            public readonly VirtualKeyboardKeyKind Kind;
            public readonly string Character;
            public readonly string Shifted;
            public readonly string Label;
            public readonly float Width;

            public Key(string character, string shifted, float width = 1f)
            {
                Kind = VirtualKeyboardKeyKind.Character;
                Character = character;
                Shifted = shifted;
                Label = character;
                Width = width;
            }

            public Key(VirtualKeyboardKeyKind kind, string label, float width)
            {
                Kind = kind;
                Character = string.Empty;
                Shifted = string.Empty;
                Label = label;
                Width = width;
            }
        }

        /// <summary>キーの並び。URL を打てることを優先した配列にしてある。</summary>
        private static readonly Key[][] Rows =
        {
            new[]
            {
                new Key("1", "!"), new Key("2", "@"), new Key("3", "#"), new Key("4", "$"),
                new Key("5", "%"), new Key("6", "^"), new Key("7", "&"), new Key("8", "*"),
                new Key("9", "("), new Key("0", ")"),
            },
            new[]
            {
                new Key("q", "Q"), new Key("w", "W"), new Key("e", "E"), new Key("r", "R"),
                new Key("t", "T"), new Key("y", "Y"), new Key("u", "U"), new Key("i", "I"),
                new Key("o", "O"), new Key("p", "P"),
            },
            new[]
            {
                new Key("a", "A"), new Key("s", "S"), new Key("d", "D"), new Key("f", "F"),
                new Key("g", "G"), new Key("h", "H"), new Key("j", "J"), new Key("k", "K"),
                new Key("l", "L"), new Key(":", ";"),
            },
            new[]
            {
                new Key("z", "Z"), new Key("x", "X"), new Key("c", "C"), new Key("v", "V"),
                new Key("b", "B"), new Key("n", "N"), new Key("m", "M"), new Key(".", "?"),
                new Key(",", "~"), new Key("/", "\\"),
            },
            new[]
            {
                new Key(VirtualKeyboardKeyKind.Shift, "Shift", 1.6f),
                new Key("-", "+"), new Key("_", "="),
                new Key(VirtualKeyboardKeyKind.Space, "Space", 2.4f),
                new Key(VirtualKeyboardKeyKind.Backspace, "Back", 1.6f),
                new Key(VirtualKeyboardKeyKind.Clear, "Clear", 1.6f),
                new Key(VirtualKeyboardKeyKind.Enter, "Enter", 1.8f),
            },
        };

        [MenuItem("YummyVerse/UI/Rebuild Virtual Keyboard")]
        public static void Rebuild()
        {
            var backplatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(BackplatePrefabGuid));
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (backplatePrefab == null || font == null)
            {
                Debug.LogError("[VirtualKeyboardBuilder] 土台のプレハブかフォントが見つかりません。");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var inputField = root.transform.Find(InputFieldPath)?.GetComponent<TMP_InputField>();
                if (inputField == null)
                {
                    Debug.LogError($"[VirtualKeyboardBuilder] {InputFieldPath} に TMP_InputField がありません。");
                    return;
                }

                var existing = root.transform.Find(KeyboardName);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                WarnOnMissingGlyphs(font);

                var keyboard = BuildKeyboard(root.transform, backplatePrefab, font, out var shiftGraphic);

                var panel = keyboard.AddComponent<VirtualKeyboardPanelView>();
                Apply(panel, so =>
                {
                    so.FindProperty("inputField").objectReferenceValue = inputField;
                    so.FindProperty("shiftKeyGraphic").objectReferenceValue = shiftGraphic;
                    so.FindProperty("shiftOffColor").colorValue = KeyColor;
                    so.FindProperty("shiftOnColor").colorValue = ShiftOnColor;
                });
                keyboard.SetActive(false);

                var view = root.transform.Find("View").GetComponent<VirtualKeyboardView>();
                Apply(view, so =>
                {
                    so.FindProperty("keyboard").objectReferenceValue = panel;
                    so.FindProperty("inputField").objectReferenceValue = inputField;
                });

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[VirtualKeyboardBuilder] {PrefabPath} に仮想キーボードを生成しました。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject BuildKeyboard(
            Transform parent, GameObject backplatePrefab, TMP_FontAsset font, out Graphic shiftGraphic)
        {
            var keyboard = (GameObject)PrefabUtility.InstantiatePrefab(backplatePrefab, parent);
            keyboard.name = KeyboardName;

            // 土台のプレハブは自前の UIThemeManager を持つが、テーマは設定ダイアログ側の
            // UIThemeManager が子まで面倒を見るので、二重に持たせない
            // (_themes が空のまま Start に入るとエラーログも出る)。
            var theme = keyboard.GetComponent<UIThemeManager>();
            if (theme != null) Object.DestroyImmediate(theme, true);

            var t = keyboard.transform;
            t.localPosition = LocalPosition;
            t.localRotation = Quaternion.Euler(LocalEulerAngles);
            t.localScale = Vector3.one;

            var canvasRoot = (RectTransform)keyboard.transform.Find("CanvasRoot");
            canvasRoot.sizeDelta = CanvasSize;

            // UIBackplate の大きさは CanvasRoot の HorizontalLayoutGroup が決める。
            // ここを素通しにして、板が Canvas いっぱいに広がるようにする。
            var canvasLayout = canvasRoot.GetComponent<HorizontalLayoutGroup>();
            canvasLayout.padding = new RectOffset(0, 0, 0, 0);
            canvasLayout.spacing = 0f;
            canvasLayout.childControlWidth = true;
            canvasLayout.childControlHeight = true;
            canvasLayout.childForceExpandWidth = true;
            canvasLayout.childForceExpandHeight = true;

            var backplate = (RectTransform)canvasRoot.Find("UIBackplate");
            var layout = backplate.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                BackplatePadding, BackplatePadding, BackplatePadding, BackplatePadding);
            layout.spacing = RowSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // GradientEffect はレイアウトの1行として数えられてしまうので、行の外に出す。
            var gradient = backplate.Find("GradientEffect");
            if (gradient != null) gradient.GetComponent<LayoutElement>().ignoreLayout = true;

            shiftGraphic = null;
            for (var r = 0; r < Rows.Length; r++)
            {
                var row = CreateRow(backplate, r);
                foreach (var key in Rows[r])
                {
                    var graphic = CreateKey(row, key, font);
                    if (key.Kind == VirtualKeyboardKeyKind.Shift) shiftGraphic = graphic;
                }
            }

            // 非アクティブのままだと LayoutGroup が一度も走らず、キーの大きさが
            // 既定の 100x100 で保存されてしまう。閉じる前にここで確定させておく。
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRoot);

            return keyboard;
        }

        private static RectTransform CreateRow(Transform parent, int index)
        {
            var go = new GameObject($"Row{index + 1}", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = KeySpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // 行の高さは均等割り。キーの幅だけを LayoutElement の flexibleWidth で配分する。
            go.AddComponent<LayoutElement>().flexibleHeight = 1f;
            return rect;
        }

        private static Graphic CreateKey(Transform parent, Key key, TMP_FontAsset font)
        {
            var go = new GameObject(KeyObjectName(key), typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.tag = "QDSUIAccentColor"; // テーマ再適用時もキー色 (primaryButton.normal) を保つ

            var image = go.AddComponent<Image>();
            image.material = RoundedBoxMaterial();
            image.color = KeyColor;
            go.AddComponent<RoundedBoxUIProperties>().borderRadius =
                new Vector4(KeyBorderRadius, KeyBorderRadius, KeyBorderRadius, KeyBorderRadius);

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            // ColorTint は Image の色に乗算されるので、normal は白のままにしてテーマ色を殺さない。
            // selected を normal と同じにしておかないと、押したキーが選択色で残る。
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f),
                pressedColor = new Color(0.70f, 0.70f, 0.70f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(1f, 1f, 1f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.05f,
            };

            go.AddComponent<LayoutElement>().flexibleWidth = key.Width;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            labelGo.tag = "QDSUITextInvertedColor"; // 明るいキーの上に暗い文字
            Stretch((RectTransform)labelGo.transform);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = key.Label;
            label.fontSize = KeyFontSize;
            label.color = KeyLabelColor;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var view = go.AddComponent<VirtualKeyboardKeyView>();
            Apply(view, so =>
            {
                so.FindProperty("kind").enumValueIndex = (int)key.Kind;
                so.FindProperty("character").stringValue = key.Character;
                so.FindProperty("shiftedCharacter").stringValue = key.Shifted;
                so.FindProperty("label").objectReferenceValue = label;
                so.FindProperty("button").objectReferenceValue = button;
            });

            return image;
        }

        /// <summary>
        /// フォントに無い文字を使うと、実行時は豆腐(□)になるだけで気づけない。
        /// (⌫ U+232B はこのフォントに入っていない、など)ので生成時に見つける。
        /// </summary>
        private static void WarnOnMissingGlyphs(TMP_FontAsset font)
        {
            var missing = new HashSet<char>();
            foreach (var row in Rows)
            {
                foreach (var key in row)
                {
                    foreach (var text in new[] { key.Label, key.Character, key.Shifted })
                    {
                        if (string.IsNullOrEmpty(text)) continue;
                        foreach (var ch in text)
                        {
                            if (!font.HasCharacter(ch)) missing.Add(ch);
                        }
                    }
                }
            }

            if (missing.Count == 0) return;
            Debug.LogWarning($"[VirtualKeyboardBuilder] {font.name} に無い文字が {missing.Count} 件あります: "
                             + string.Join(" ", missing));
        }

        /// <summary>
        /// 記号をそのまま GameObject 名に使うと、'/' が Transform.Find のパス区切りと
        /// 衝突して二度と引けなくなる。英数字以外は名前に置き換える。
        /// </summary>
        private static readonly Dictionary<char, string> SymbolNames = new()
        {
            ['/'] = "Slash",
            [':'] = "Colon",
            ['.'] = "Period",
            [','] = "Comma",
            ['-'] = "Minus",
            ['_'] = "Underscore",
        };

        private static string KeyObjectName(Key key)
        {
            if (key.Kind != VirtualKeyboardKeyKind.Character) return $"Key_{key.Kind}";

            var c = key.Character[0];
            if (char.IsLetterOrDigit(c)) return $"Key_{key.Character}";

            return SymbolNames.TryGetValue(c, out var name)
                ? $"Key_{name}"
                : $"Key_U{((int)c):X4}";
        }

        /// <summary>設定ダイアログのボタンと同じ角丸マテリアルを使い回す。</summary>
        private static Material RoundedBoxMaterial()
        {
            foreach (var guid in AssetDatabase.FindAssets("RoundedBoxUI t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && material.name == "RoundedBoxUI") return material;
            }

            Debug.LogWarning("[VirtualKeyboardBuilder] RoundedBoxUI マテリアルが見つかりません。");
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void Apply(Object target, System.Action<SerializedObject> configure)
        {
            var so = new SerializedObject(target);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
