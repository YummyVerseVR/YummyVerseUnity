using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct.SO.Tutorial;

namespace YummyVerse.Scripts.Model
{
    public class TutorialAnalytics : ITutorialAnalytics
    {
        public void Record(string stepId, TutorialStepPhase phase, float elapsedSeconds)
        {
            Debug.Log($"[TutorialAnalytics] {stepId} {phase} ({elapsedSeconds:F1}s)");
        }
    }
}
