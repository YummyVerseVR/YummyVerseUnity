using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Video;

namespace YummyVerse.Scripts.ViewModel.Tutorial.SO
{
    public enum HintMode
    {
        Text,
        Video,
        Both
    }

    /// <summary>
    /// TaskStep で来場者が滞留したときに出すヒント。
    /// </summary>
    [CreateAssetMenu(fileName = "Hint_", menuName = "YummyVerse/Tutorial/Hint Presentation")]
    public class HintPresentation : ScriptableObject
    {
        [SerializeField] private HintMode mode = HintMode.Both;
        [SerializeField] private LocalizedString text;
        [SerializeField] private VideoClip demoClip;

        public HintMode Mode => mode;
        public LocalizedString Text => text;

        /// <summary>Text モードのときは動画を出さない。</summary>
        public VideoClip DemoClip => mode == HintMode.Text ? null : demoClip;

        public bool ShowsText => mode != HintMode.Video;
    }
}
