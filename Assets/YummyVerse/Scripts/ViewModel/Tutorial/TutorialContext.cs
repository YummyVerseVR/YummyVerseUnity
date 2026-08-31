using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    /// <summary>
    /// ステップが触ってよいものを1箇所に集めたもの。
    /// ステップはここに載っていないものへは一切アクセスしない。
    /// </summary>
    public class TutorialContext
    {
        public IGameEventBus Events { get; }
        public IGameCommandBus Commands { get; }
        public IMessagePresenter Message { get; }
        public IHintPresenter Hint { get; }
        public IFeedbackPresenter Feedback { get; }
        public IVoicePresenter Voice { get; }
        public IChoicePresenter Choice { get; }
        public ITutorialAnalytics Analytics { get; }

        public bool IsFirstTimeUser { get; set; } = true;

        /// <summary>選択結果など、ステップ間で受け渡したい値。</summary>
        public IDictionary<string, object> Blackboard { get; } = new Dictionary<string, object>();

        /// <summary>
        /// ChoiceStep が分岐先のサブシーケンスを実行するための口。TutorialRunner が差し込む。
        /// 任意のステップへジャンプする仕組みは意図的に持たせていない。
        /// </summary>
        public Func<TutorialSequence, CancellationToken, UniTask> RunSubSequenceAsync { get; set; }

        /// <summary>TaskStep の ReturnToAttract 救済からセッション中断を要求する。</summary>
        public event Action AbortRequested;

        public TutorialContext(
            IGameEventBus events,
            IGameCommandBus commands,
            IMessagePresenter message,
            IHintPresenter hint,
            IFeedbackPresenter feedback,
            IVoicePresenter voice,
            IChoicePresenter choice,
            ITutorialAnalytics analytics)
        {
            Events = events;
            Commands = commands;
            Message = message;
            Hint = hint;
            Feedback = feedback;
            Voice = voice;
            Choice = choice;
            Analytics = analytics;
        }

        public void RequestAbort() => AbortRequested?.Invoke();

        /// <summary>
        /// セッションをまたいで状態が汚染されないように、セッション開始前に必ず呼ぶ。
        /// </summary>
        public void ResetForNewSession()
        {
            IsFirstTimeUser = true;
            Blackboard.Clear();
        }

        public bool TryGetBlackboard<T>(string key, out T value)
        {
            if (!string.IsNullOrEmpty(key) && Blackboard.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }
}
