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
        [Tooltip("仕様書 S1「親指の位置のボタンを押してスタート」に相当。ここで A/B ボタンを待つ。")]
        [SerializeField] private LocalizedString attractMessage;
        [SerializeField] private AudioClip attractVoiceClip;

        [Tooltip("食べ物の表示位置が未設定のときに、Attract の前へ割り込ませる案内。" +
                 "設定画面で位置が決まるまでセッションを開始させない。")]
        [SerializeField] private LocalizedString foodPlacementRequiredMessage;

        [Header("咀嚼計のキャリブレーション (スタート直後、S2 の手前)")]
        [Tooltip("CAL_START を送ってから CAL_ACCEPTED 待ちの間に出す案内。")]
        [SerializeField] private LocalizedString chewingCalibrationHoldMessage;

        [Tooltip("CAL_ACCEPTED 受信後、下の秒数だけ待ってから出す案内。")]
        [SerializeField] private LocalizedString chewingCalibrationChewMessage;

        [Tooltip("CAL_ACCEPTED から「もぐもぐしてください」へ切り替えるまでの待ち時間。")]
        [SerializeField, Min(0f)] private float chewingCalibrationChewPromptDelaySeconds = 5f;

        [Header("無操作の監視")]
        [Tooltip("この秒数だけ何も起きなければ UserAbsent としてセッションを中断する")]
        [SerializeField, Min(1f)] private float idleTimeoutSeconds = 90f;

        public TutorialSequence MainSequence => mainSequence;
        public TutorialSequence FreePlaySequence => freePlaySequence;
        public LocalizedString AttractMessage => attractMessage;
        public AudioClip AttractVoiceClip => attractVoiceClip;
        public LocalizedString FoodPlacementRequiredMessage => foodPlacementRequiredMessage;
        public LocalizedString ChewingCalibrationHoldMessage => chewingCalibrationHoldMessage;
        public LocalizedString ChewingCalibrationChewMessage => chewingCalibrationChewMessage;
        public float ChewingCalibrationChewPromptDelaySeconds => chewingCalibrationChewPromptDelaySeconds;
        public float IdleTimeoutSeconds => idleTimeoutSeconds;
    }
}
