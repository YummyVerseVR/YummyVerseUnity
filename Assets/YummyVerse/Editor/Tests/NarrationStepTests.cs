using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;
using YummyVerse.Scripts.ViewModel.Tutorial.SO.Steps;

namespace YummyVerse.Editor.Tests
{
    public class NarrationStepTests
    {
        [Test]
        public void PresentationCondition_IsSatisfiedBeforeMessageIsShown()
        {
            var step = ScriptableObject.CreateInstance<NarrationStep>();
            var presentationCondition = ScriptableObject.CreateInstance<ManualTutorialCondition>();
            var completionCondition = ScriptableObject.CreateInstance<ManualTutorialCondition>();
            var message = new RecordingMessagePresenter();
            var voice = new RecordingVoicePresenter();
            var commands = new RecordingGameCommandBus();

            try
            {
                var serializedStep = new SerializedObject(step);
                serializedStep.FindProperty("presentationCondition").objectReferenceValue = presentationCondition;
                serializedStep.FindProperty("completionCondition").objectReferenceValue = completionCondition;
                serializedStep.ApplyModifiedPropertiesWithoutUndo();

                var context = new TutorialContext(
                    null,
                    commands,
                    message,
                    null,
                    null,
                    voice,
                    null,
                    null);

                var execution = step.ExecuteAsync(context, CancellationToken.None);

                Assert.That(presentationCondition.HasStarted, Is.True);
                Assert.That(message.ShowCount, Is.Zero);

                presentationCondition.Complete();

                Assert.That(message.ShowCount, Is.EqualTo(1));
                Assert.That(completionCondition.HasStarted, Is.True);

                completionCondition.Complete();
                execution.GetAwaiter().GetResult();
            }
            finally
            {
                message.Dispose();
                voice.Dispose();
                commands.Dispose();
                Object.DestroyImmediate(step);
                Object.DestroyImmediate(presentationCondition);
                Object.DestroyImmediate(completionCondition);
            }
        }

        [Test]
        public void FreePlayOutro_WaitsForDishClearedThenShowsEachMessageForFiveSeconds()
        {
            var thankYou = AssetDatabase.LoadAssetAtPath<NarrationStep>(
                "Assets/YummyVerse/Data/Tutorial/Steps/Step_S18_ThankYou.asset");
            var farewell = AssetDatabase.LoadAssetAtPath<NarrationStep>(
                "Assets/YummyVerse/Data/Tutorial/Steps/Step_S19_Farewell.asset");
            var dishCleared = AssetDatabase.LoadAssetAtPath<TutorialCondition>(
                "Assets/YummyVerse/Data/Tutorial/Conditions/Cond_DishCleared.asset");
            var fiveSeconds = AssetDatabase.LoadAssetAtPath<TutorialCondition>(
                "Assets/YummyVerse/Data/Tutorial/Conditions/Cond_Time5s.asset");

            Assert.That(thankYou, Is.Not.Null);
            Assert.That(farewell, Is.Not.Null);

            var thankYouProperties = new SerializedObject(thankYou);
            var farewellProperties = new SerializedObject(farewell);

            Assert.That(
                thankYouProperties.FindProperty("presentationCondition").objectReferenceValue,
                Is.SameAs(dishCleared));
            Assert.That(
                thankYouProperties.FindProperty("completionCondition").objectReferenceValue,
                Is.SameAs(fiveSeconds));
            Assert.That(
                farewellProperties.FindProperty("completionCondition").objectReferenceValue,
                Is.SameAs(fiveSeconds));
        }

        private sealed class RecordingMessagePresenter : IMessagePresenter, System.IDisposable
        {
            public ReactiveProperty<string> Text { get; } = new(string.Empty);
            public ReactiveProperty<bool> IsVisible { get; } = new(false);
            public int ShowCount { get; private set; }

            public UniTask ShowAsync(LocalizedString msg, CancellationToken ct) => ShowAsync(msg, null, ct);

            public UniTask ShowAsync(LocalizedString msg, string subText, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                ShowCount++;
                SubText = subText;
                IsVisible.Value = true;
                return UniTask.CompletedTask;
            }

            public string SubText { get; private set; }

            public void SetSubText(string subText) => SubText = subText;

            public UniTask HideAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                IsVisible.Value = false;
                return UniTask.CompletedTask;
            }

            public void Dispose()
            {
                Text.Dispose();
                IsVisible.Dispose();
            }
        }

        private sealed class RecordingVoicePresenter : IVoicePresenter, System.IDisposable
        {
            private readonly Subject<AudioClip> _onPlay = new();
            private readonly Subject<Unit> _onStop = new();

            public Observable<AudioClip> OnPlay => _onPlay;
            public Observable<Unit> OnStop => _onStop;

            public UniTask PlayAsync(AudioClip clip, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public void Stop() => _onStop.OnNext(Unit.Default);

            public void Dispose()
            {
                _onPlay.Dispose();
                _onStop.Dispose();
            }
        }

        private sealed class RecordingGameCommandBus : IGameCommandBus, System.IDisposable
        {
            private readonly Subject<GameCommandId> _onCommand = new();

            public Observable<GameCommandId> OnCommand => _onCommand;

            public void Request(GameCommandId command) => _onCommand.OnNext(command);

            public void Dispose() => _onCommand.Dispose();
        }

        private sealed class ManualTutorialCondition : TutorialCondition
        {
            private readonly UniTaskCompletionSource _completion = new();

            public bool HasStarted { get; private set; }

            public override UniTask WaitAsync(TutorialContext ctx, CancellationToken ct)
            {
                HasStarted = true;
                return _completion.Task.AttachExternalCancellation(ct);
            }

            public void Complete() => _completion.TrySetResult();
        }
    }
}
