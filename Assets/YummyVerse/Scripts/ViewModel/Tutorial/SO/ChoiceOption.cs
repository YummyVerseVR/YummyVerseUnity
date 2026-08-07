using System;
using UnityEngine;
using UnityEngine.Localization;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO
{
    /// <summary>
    /// ChoiceStep が IsFirstTimeUser に与える影響。
    /// 仕様書 S6「初回かどうかの判定」をコードにハードコードせずに表現するためのもの。
    /// </summary>
    public enum FirstTimeUserEffect
    {
        None,
        SetTrue,
        SetFalse
    }

    [Serializable]
    public class ChoiceOption
    {
        [SerializeField] private LocalizedString label;

        [Tooltip("Blackboard に格納される値")]
        [SerializeField] private string value;

        [Tooltip("この選択肢が選ばれたときに実行するサブシーケンス (任意)")]
        [SerializeField] private TutorialSequence subSequence;

        [SerializeField] private FirstTimeUserEffect firstTimeUserEffect = FirstTimeUserEffect.None;

        public LocalizedString Label => label;
        public string Value => value;
        public TutorialSequence SubSequence => subSequence;
        public FirstTimeUserEffect FirstTimeUserEffect => firstTimeUserEffect;
    }
}
