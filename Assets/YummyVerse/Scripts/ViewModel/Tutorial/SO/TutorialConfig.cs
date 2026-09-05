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
        [Tooltip("ノイズ測定フェーズの案内。カウントが 0 になった時点で CAL_NOISE を送る。")]
        [SerializeField] private LocalizedString chewingCalibrationNoiseMessage;

        [Tooltip("咀嚼測定フェーズの案内。カウントが 0 になった時点で CAL_CHEW を送る。")]
        [SerializeField] private LocalizedString chewingCalibrationChewMessage;

        [Tooltip("カウントが 0 になってから測定完了までの間、案内の下に出す表示。")]
        [SerializeField] private LocalizedString chewingCalibrationMeasuringMessage;

        [Tooltip("各フェーズの案内を出してから測定を始めるまでのカウントダウン秒数 (仕様書 §9.2)。")]
        [SerializeField, Min(0)] private int chewingCalibrationCountdownSeconds = 5;

        [Header("無操作の監視")]
        [Tooltip("この秒数だけ何も起きなければ UserAbsent としてセッションを中断する")]
        [SerializeField, Min(1f)] private float idleTimeoutSeconds = 90f;

        public TutorialSequence MainSequence => mainSequence;
        public TutorialSequence FreePlaySequence => freePlaySequence;
        public LocalizedString AttractMessage => attractMessage;
        public AudioClip AttractVoiceClip => attractVoiceClip;
        public LocalizedString FoodPlacementRequiredMessage => foodPlacementRequiredMessage;
        public LocalizedString ChewingCalibrationNoiseMessage => chewingCalibrationNoiseMessage;
        public LocalizedString ChewingCalibrationChewMessage => chewingCalibrationChewMessage;
        public LocalizedString ChewingCalibrationMeasuringMessage => chewingCalibrationMeasuringMessage;
        public int ChewingCalibrationCountdownSeconds => chewingCalibrationCountdownSeconds;
        public float IdleTimeoutSeconds => idleTimeoutSeconds;
    }
}
