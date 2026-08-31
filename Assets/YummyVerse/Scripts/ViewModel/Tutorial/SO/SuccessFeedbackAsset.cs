using UnityEngine;
using UnityEngine.Localization;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO
{
    /// <summary>
    /// TaskStep 達成時の「OK!」演出。全 TaskStep で同じアセットを使い回す想定。
    /// </summary>
    [CreateAssetMenu(fileName = "SuccessFeedback_", menuName = "YummyVerse/Tutorial/Success Feedback")]
    public class SuccessFeedbackAsset : ScriptableObject
    {
        [SerializeField] private LocalizedString label;
        [SerializeField] private AudioClip sfx;
        [SerializeField, Min(0f)] private float durationSeconds = 1.2f;

        public LocalizedString Label => label;
        public AudioClip Sfx => sfx;
        public float DurationSeconds => durationSeconds;
    }
}
