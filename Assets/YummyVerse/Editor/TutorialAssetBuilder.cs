using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO.Tutorial;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;
using YummyVerse.Scripts.ViewModel.Tutorial.SO.Conditions;
using YummyVerse.Scripts.ViewModel.Tutorial.SO.Steps;

namespace YummyVerse.Editor
{
    /// <summary>
    /// 仕様書 §6 のステップ定義データを ScriptableObject として一括生成する。
    /// 文言・秒数・順序はここで作ったあと、コードを触らずにインスペクタで差し替えられる。
    ///
    /// 既存のアセットは上書きしないので、何度実行しても安全。
    /// </summary>
    public static class TutorialAssetBuilder
    {
        private const string RootPath = "Assets/YummyVerse/Data/Tutorial";
        private const string TableName = "TutorialStrings";
        private const string TableGroupPath = RootPath + "/Localization";

        // 自前の MenuItem 構造体と名前が衝突するため、属性側を明示的に修飾する。
        [UnityEditor.MenuItem("YummyVerse/Tutorial/Create Default Tutorial Assets")]
        public static void CreateAll()
        {
            EnsureFolders();
            var table = EnsureStringTable();
            if (table == null) return;

            var conditions = CreateConditions();
            var feedback = CreateSuccessFeedback(table);
            var steps = CreateSteps(table, conditions, feedback);
            CreateSequencesAndConfig(table, steps);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TutorialAssetBuilder] {RootPath} 以下にチュートリアルアセットを生成しました。");
        }

        // ------------------------------------------------------------------
        // フォルダ / ローカライズ
        // ------------------------------------------------------------------

        private static void EnsureFolders()
        {
            foreach (var path in new[]
                     {
                         RootPath,
                         RootPath + "/Conditions",
                         RootPath + "/Steps",
                         RootPath + "/Sequences",
                         RootPath + "/Feedback",
                         TableGroupPath
                     })
            {
                CreateFolderRecursive(path);
            }
        }

        private static void CreateFolderRecursive(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            CreateFolderRecursive(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static StringTableCollection EnsureStringTable()
        {
            if (LocalizationSettings.Instance == null)
            {
                Debug.LogError(
                    "[TutorialAssetBuilder] Localization Settings がありません。" +
                    "Edit > Project Settings > Localization から作成してから再実行してください。");
                return null;
            }

            var existing = LocalizationEditorSettings.GetStringTableCollection(TableName);
            if (existing != null) return existing;

            if (!LocalizationEditorSettings.GetLocales().Any())
            {
                Debug.LogError(
                    "[TutorialAssetBuilder] Locale が1つも登録されていません。" +
                    "Localization Settings で日本語(ja)を追加してから再実行してください。");
                return null;
            }

            return LocalizationEditorSettings.CreateStringTableCollection(TableName, TableGroupPath);
        }

        /// <summary>
        /// キーに日本語の原文を入れた LocalizedString を返す。
        /// 既にキーがあれば原文は上書きしない(現場で直した文言を消さないため)。
        /// </summary>
        private static LocalizedString Str(StringTableCollection collection, string key, string japanese)
        {
            var sharedData = collection.SharedData;
            var entry = sharedData.GetEntry(key) ?? sharedData.AddKey(key);

            foreach (var table in collection.StringTables)
            {
                var tableEntry = table.GetEntry(entry.Id);
                if (tableEntry != null && !string.IsNullOrEmpty(tableEntry.Value)) continue;

                // 原文は日本語。他ロケールは翻訳が入るまで同じ文字列を入れておく。
                table.AddEntry(entry.Id, japanese);
                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(sharedData);

            return new LocalizedString(collection.SharedData.TableCollectionName, key);
        }

        // ------------------------------------------------------------------
        // 生成本体
        // ------------------------------------------------------------------

        private class Conditions
        {
            public ButtonPressedCondition Button;
            public TimeElapsedCondition Time3;
            public TimeElapsedCondition Time5;
            public AnyOfCondition ButtonOr8;
            public GameEventCondition FoodScooped;
            public GameEventCondition DishCleared;
        }

        private static Conditions CreateConditions()
        {
            var c = new Conditions
            {
                Button = Create<ButtonPressedCondition>("Conditions/Cond_ButtonPressed"),
                Time3 = Create<TimeElapsedCondition>("Conditions/Cond_Time3s"),
                Time5 = Create<TimeElapsedCondition>("Conditions/Cond_Time5s"),
                ButtonOr8 = Create<AnyOfCondition>("Conditions/Cond_ButtonOr8s"),
                FoodScooped = Create<GameEventCondition>("Conditions/Cond_FoodScooped"),
                DishCleared = Create<GameEventCondition>("Conditions/Cond_DishCleared")
            };

            SetField(c.Time3, "seconds", 3f);
            SetField(c.Time5, "seconds", 5f);

            SetEnum(c.FoodScooped, "eventId", GameEventId.FoodScooped);
            SetEnum(c.DishCleared, "eventId", GameEventId.DishCleared);

            // 「ボタン押下 または 8秒経過で進む」を新クラスなしで表現する
            var time8 = Create<TimeElapsedCondition>("Conditions/Cond_Time8s");
            SetField(time8, "seconds", 8f);
            SetList(c.ButtonOr8, "conditions", new TutorialCondition[] { c.Button, time8 });

            return c;
        }

        private static SuccessFeedbackAsset CreateSuccessFeedback(StringTableCollection table)
        {
            var asset = Create<SuccessFeedbackAsset>("Feedback/SuccessFeedback_OK");
            SetField(asset, "label", Str(table, "Feedback.OK", "OK!"));
            SetField(asset, "durationSeconds", 1.2f);
            return asset;
        }

        private static Dictionary<string, TutorialStep> CreateSteps(
            StringTableCollection table, Conditions conditions, SuccessFeedbackAsset feedback)
        {
            var steps = new Dictionary<string, TutorialStep>();

            // --- S2: ようこそ (ボタン or 時間) ---
            steps["S2"] = Narration(
                "Steps/Step_S2_Welcome", "S2",
                Str(table, "S2", "YummyVerse へようこそ。\nAIが生み出した食感を体験していただきます。"),
                conditions.ButtonOr8);

            // --- S5: AIシェフの準備 (時間) ---
            // 前菜の提供は S8 の指示と同時。ここで出すと初回判定の
            // ダイアログが出ている最中に食べ物が現れてしまう。
            var s5 = Narration(
                "Steps/Step_S5_ChefReady", "S5",
                Str(table, "S5", "AIシェフの準備ができたようです。"),
                conditions.Time3);
            SetEnum(s5, "onCompletedCommand", GameCommandId.None);
            steps["S5"] = s5;

            // --- S6: 初回かどうかの判定 (Choice) ---
            var s6 = Create<ChoiceStep>("Steps/Step_S6_FirstTimeCheck");
            SetField(s6, "stepId", "S6");
            SetField(s6, "prompt", Str(table, "S6.Prompt", "YummyVerse のご利用は初めてですか？\n人差し指のボタンで選択してください。"));
            SetField(s6, "timeoutSeconds", 15f);
            SetField(s6, "defaultOptionIndex", 0);
            SetField(s6, "blackboardKey", "isFirstTime");
            SetList(s6, "options", new[]
            {
                Option(Str(table, "S6.Yes", "はじめて"), "yes", FirstTimeUserEffect.SetTrue),
                Option(Str(table, "S6.No", "2回目以降"), "no", FirstTimeUserEffect.SetFalse)
            });
            steps["S6"] = s6;

            // S6' 〜 S14 は前菜での練習。「2回目以降」を選んだ来場者は skippableOnRepeat で
            // ここを丸ごと飛ばし、Main シーケンス終了後の FreePlay(食べ物選択ダイアログ)へ直行する。

            // --- S6': 前菜の案内 (食品は次の S8 の指示と同時に提供する) ---
            var s6d = Narration(
                "Steps/Step_S6d_Appetizer", "S6'",
                Str(table, "S6d", "まずはリンゴを食べてみましょう。\nAボタンを押してください。"),
                conditions.Button);
            SetEnum(s6d, "onCompletedCommand", GameCommandId.None);
            SetField(s6d, "skippableOnRepeat", true);
            steps["S6'"] = s6d;

            // --- S8: スプーンですくう (Task / すくわれた) ---
            // 進行条件は実際の FoodScooped だけ。滞留しても時間で素通りさせず、
            // ゲーム側に1回すくわせて本物のイベントで先へ進める。
            var s8 = Create<TaskStep>("Steps/Step_S8_Scoop");
            SetField(s8, "stepId", "S8");
            SetField(s8, "instruction", Str(table, "S8", "スプーンで食べ物をすくって口に入れ\nもぐもぐしてください"));
            // S7: すくう指示が出たタイミングで前菜を提供する。
            SetEnum(s8, "onStartedCommand", GameCommandId.ServeRandomPersistentFood);
            SetField(s8, "successCondition", conditions.FoodScooped);
            SetField(s8, "hintDelaySeconds", 5f);
            SetField(s8, "rescueTimeoutSeconds", 30f);
            SetEnum(s8, "rescuePolicy", RescuePolicy.ForceComplete);
            SetEnum(s8, "forceCompleteCommand", GameCommandId.ForceScoopFood);
            SetField(s8, "successFeedback", feedback);
            SetField(s8, "skippableOnRepeat", true);
            steps["S8"] = s8;

            // --- S11: 完食しよう (Task / 完食) ---
            var s11 = Create<TaskStep>("Steps/Step_S11_ClearDish");
            SetField(s11, "stepId", "S11");
            SetField(s11, "instruction", Str(table, "S11", "その調子です。このまま完食してみましょう。"));
            SetField(s11, "successCondition", conditions.DishCleared);
            SetField(s11, "hintDelaySeconds", 8f);
            SetField(s11, "rescueTimeoutSeconds", 45f);
            SetEnum(s11, "rescuePolicy", RescuePolicy.ForceComplete);
            SetEnum(s11, "forceCompleteCommand", GameCommandId.ForceClearDish);
            SetField(s11, "successFeedback", feedback);
            SetField(s11, "skippableOnRepeat", true);
            steps["S11"] = s11;

            // --- S14: 食事作法はお分かりいただけましたか (時間) ---
            var s14 = Narration(
                "Steps/Step_S14_Recap", "S14",
                Str(table, "S14", "食事作法はお分かりいただけましたか。"),
                conditions.Time3);
            SetField(s14, "skippableOnRepeat", true);
            steps["S14"] = s14;

            // ================= FreePlay (S15〜S19) =================

            steps["S15"] = Narration(
                "Steps/Step_S15_OrderPrompt", "S15",
                Str(table, "S15", "それでは、食べたいモノを注文してみましょう。"),
                conditions.Button);

            var s16 = Create<ChoiceStep>("Steps/Step_S16_Menu");
            SetField(s16, "stepId", "S16");
            SetField(s16, "prompt", Str(table, "S16.Prompt", "お好きなメニューをお選びください。"));
            SetField(s16, "timeoutSeconds", 30f);
            SetField(s16, "defaultOptionIndex", 0);
            SetField(s16, "blackboardKey", "menu");
            SetList(s16, "options", new[]
            {
                Option(Str(table, "S16.Shrimp", "エビ"), "Shrimp", FirstTimeUserEffect.None),
                Option(Str(table, "S16.Curry", "カレー"), "Curry", FirstTimeUserEffect.None),
                Option(Str(table, "S16.Hamburg", "ハンバーグ"), "Hamburg", FirstTimeUserEffect.None),
                Option(Str(table, "S16.DragonSteak", "ドラゴンステーキ"), "DragonSteak", FirstTimeUserEffect.None)
            });
            steps["S16"] = s16;

            // S17 の提供はメニュー選択(既存の SpawnLocalFood)で行われるため専用ステップは作らない。
            var s18 = Narration(
                "Steps/Step_S18_ThankYou", "S18",
                Str(table, "S18", "完食ありがとうございました。"),
                conditions.Time5);
            SetField(s18, "presentationCondition", conditions.DishCleared);
            steps["S18"] = s18;

            steps["S19"] = Narration(
                "Steps/Step_S19_Farewell", "S19",
                Str(table, "S19", "またのご来店をお待ちしております。"),
                conditions.Time5);

            return steps;
        }

        private static void CreateSequencesAndConfig(StringTableCollection table, IReadOnlyDictionary<string, TutorialStep> steps)
        {
            var main = Create<TutorialSequence>("Sequences/TutorialSequence_Main");
            SetList(main, "steps", new[]
            {
                // S3 の「紙皿を見つめる」案内は現行フローから削除済み。
                steps["S2"], steps["S5"], steps["S6"],
                steps["S6'"], steps["S8"], steps["S11"], steps["S14"]
            });

            var freePlay = Create<TutorialSequence>("Sequences/TutorialSequence_FreePlay");
            SetList(freePlay, "steps", new[]
            {
                // S15/S16 の固定4択は FoodSelectionFlow の API v2 + PersistentData gridへ置換済み。
                steps["S18"], steps["S19"]
            });

            var config = Create<TutorialConfig>("TutorialConfig");
            SetField(config, "mainSequence", main);
            SetField(config, "freePlaySequence", freePlay);
            SetField(config, "attractMessage",
                Str(table, "S1", "Aボタンを押してスタート"));
            SetField(config, "foodPlacementRequiredMessage",
                Str(table, "S0.FoodPlacementRequired",
                    "食べ物の表示位置を設定してください。\n設定画面はAとXの同時押しで表示されます。"));

            // S1 の直後、S2「ようこそ」の手前に挟まる咀嚼計の較正案内。
            // 各案内はカウントダウンを伴い、0 になった時点で測定フェーズが始まる (仕様書 §9.2)。
            SetField(config, "chewingCalibrationNoiseMessage",
                Str(table, "S1.ChewingCalibrationNoise", "小さく歯をカチカチしてください"));
            SetField(config, "chewingCalibrationChewMessage",
                Str(table, "S1.ChewingCalibrationChew", "奥歯でちゃんと噛みしめてください"));
            SetField(config, "chewingCalibrationMeasuringMessage",
                Str(table, "S1.ChewingCalibrationMeasuring", "計測中..."));
            SetField(config, "chewingCalibrationCountdownSeconds", 5);

            SetField(config, "idleTimeoutSeconds", 90f);
        }

        // ------------------------------------------------------------------
        // ヘルパー
        // ------------------------------------------------------------------

        private static NarrationStep Narration(string path, string stepId, LocalizedString message, TutorialCondition condition)
        {
            var step = Create<NarrationStep>(path);
            SetField(step, "stepId", stepId);
            SetField(step, "message", message);
            SetField(step, "completionCondition", condition);
            return step;
        }

        private static ChoiceOption Option(LocalizedString label, string value, FirstTimeUserEffect effect)
        {
            var option = new ChoiceOption();
            SetField(option, "label", label);
            SetField(option, "value", value);
            SetEnum(option, "firstTimeUserEffect", effect);
            return option;
        }

        private static T Create<T>(string relativePath) where T : ScriptableObject
        {
            var path = $"{RootPath}/{relativePath}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing; // 冪等: 既存は壊さない

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // SerializeField は private なので SerializedObject 経由で書き込む。
        private static void SetField(object target, string fieldName, object value)
        {
            var unityObject = target as Object;
            if (unityObject != null)
            {
                var so = new SerializedObject(unityObject);
                var prop = so.FindProperty(fieldName);
                if (prop == null)
                {
                    Debug.LogWarning($"[TutorialAssetBuilder] {unityObject.name}.{fieldName} が見つかりません");
                    return;
                }

                AssignProperty(prop, value);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(unityObject);
                return;
            }

            // ChoiceOption のような [Serializable] クラスはリフレクションで直接書き込む
            var field = target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance
                                     | System.Reflection.BindingFlags.NonPublic
                                     | System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                Debug.LogWarning($"[TutorialAssetBuilder] {target.GetType().Name}.{fieldName} が見つかりません");
                return;
            }

            field.SetValue(target, value);
        }

        private static void SetEnum(object target, string fieldName, System.Enum value)
        {
            SetField(target, fieldName, value);
        }

        private static void SetList<T>(Object target, string fieldName, IReadOnlyList<T> values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[TutorialAssetBuilder] {target.name}.{fieldName} が見つかりません");
                return;
            }

            prop.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                AssignProperty(prop.GetArrayElementAtIndex(i), values[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void AssignProperty(SerializedProperty prop, object value)
        {
            switch (value)
            {
                case null:
                    prop.objectReferenceValue = null;
                    break;
                case string s:
                    prop.stringValue = s;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
                case System.Enum e:
                    prop.enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(e.GetType()), e);
                    break;
                case LocalizedString localized:
                    AssignLocalizedString(prop, localized);
                    break;
                case ChoiceOption option:
                    AssignChoiceOption(prop, option);
                    break;
                case Object unityObject:
                    prop.objectReferenceValue = unityObject;
                    break;
                default:
                    Debug.LogWarning($"[TutorialAssetBuilder] 未対応の型: {value.GetType().Name}");
                    break;
            }
        }

        private static void AssignLocalizedString(SerializedProperty prop, LocalizedString value)
        {
            if (value == null) return;

            var tableName = prop.FindPropertyRelative("m_TableReference.m_TableCollectionName");
            var key = prop.FindPropertyRelative("m_TableEntryReference.m_Key");
            var keyId = prop.FindPropertyRelative("m_TableEntryReference.m_KeyId");

            if (tableName == null || key == null)
            {
                Debug.LogWarning(
                    "[TutorialAssetBuilder] LocalizedString の内部フィールド名が想定と異なります。" +
                    "生成後に手動で文言を割り当ててください。");
                return;
            }

            tableName.stringValue = value.TableReference.TableCollectionName;
            key.stringValue = value.TableEntryReference.Key;
            if (keyId != null) keyId.longValue = 0;
        }

        private static void AssignChoiceOption(SerializedProperty prop, ChoiceOption option)
        {
            AssignLocalizedString(prop.FindPropertyRelative("label"), option.Label);
            prop.FindPropertyRelative("value").stringValue = option.Value;
            prop.FindPropertyRelative("firstTimeUserEffect").enumValueIndex = (int)option.FirstTimeUserEffect;
            prop.FindPropertyRelative("subSequence").objectReferenceValue = option.SubSequence;
        }
    }
}
