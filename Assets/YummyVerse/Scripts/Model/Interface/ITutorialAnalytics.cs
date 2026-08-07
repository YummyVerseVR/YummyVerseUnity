using YummyVerse.Scripts.Model.Struct.SO.Tutorial;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// どのステップで来場者が詰まるかを後から改善するための記録口。
    /// 既定実装は Debug.Log。外部送信が必要になったらここを差し替える。
    /// </summary>
    public interface ITutorialAnalytics
    {
        void Record(string stepId, TutorialStepPhase phase, float elapsedSeconds);
    }
}
