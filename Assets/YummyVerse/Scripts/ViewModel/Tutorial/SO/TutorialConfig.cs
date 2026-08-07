using UnityEngine;
using UnityEngine.Localization;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO
{
    /// <summary>
    /// セッション全体の設定。コードを触らずに現場で調整できるようにここへ集約する。
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "YummyVerse/Tutorial/Tutorial Config")]
    public class TutorialConfig : ScriptableObject
    {
        [Header("シーケンス")]
        [Tooltip("チュートリアル本体 (S2〜S14)")]
        [SerializeField] private TutorialSequence mainSequence;

        [Tooltip("FreePlay (S15〜S19)。境界は S14 終了時点。")]
        [SerializeField] private TutorialSequence freePlaySequence;

        [Header("Attract (来場者を待つ状態)")]
        [Tooltip("仕様書 S1「ボタンを押してスタート」に相当。ここで決定ボタンを待つ。")]
        [SerializeField] private LocalizedString attractMessage;
        [SerializeField] private AudioClip attractVoiceClip;

        [Header("無操作の監視")]
        [Tooltip("この秒数だけ何も起きなければ UserAbsent としてセッションを中断する")]
        [SerializeField, Min(1f)] private float idleTimeoutSeconds = 90f;

        public TutorialSequence MainSequence => mainSequence;
        public TutorialSequence FreePlaySequence => freePlaySequence;
        public LocalizedString AttractMessage => attractMessage;
        public AudioClip AttractVoiceClip => attractVoiceClip;
        public float IdleTimeoutSeconds => idleTimeoutSeconds;
    }
}
