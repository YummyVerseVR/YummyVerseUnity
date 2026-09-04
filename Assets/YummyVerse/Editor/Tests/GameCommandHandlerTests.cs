using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using NUnit.Framework;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial;

namespace YummyVerse.Editor.Tests
{
    public sealed class GameCommandHandlerTests
    {
        [Test]
        public void InitializeRoutesShowAndHideMenuCommandsToWindowVisibility()
        {
            using var fixture = new Fixture();
            fixture.Handler.Initialize();

            fixture.CommandBus.Request(GameCommandId.ShowMenu);
            fixture.CommandBus.Request(GameCommandId.HideMenu);

            Assert.That(fixture.Window.VisibilityChanges, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void DisposeStopsHandlingCommands()
        {
            using var fixture = new Fixture();
            fixture.Handler.Initialize();
            fixture.Handler.Dispose();

            fixture.CommandBus.Request(GameCommandId.ShowMenu);

            Assert.That(fixture.Window.VisibilityChanges, Is.Empty);
        }

        [Test]
        public void ServeRandomPersistentFoodActivatesPlacementAndPublishesSelectionOnce()
        {
            var selected = new FoodCatalogItem(
                "persistent:test-food",
                "Test Food",
                "/tmp/test-food/preview.png",
                "/tmp/test-food/model.glb",
                "/tmp/test-food/audio.mp3",
                MenuItemSource.PersistentData);
            using var fixture = new Fixture(selected);
            fixture.Handler.Initialize();

            fixture.CommandBus.Request(GameCommandId.ServeRandomPersistentFood);

            Assert.That(fixture.Placement.ActivationCount, Is.EqualTo(1));
            Assert.That(fixture.EventPublisher.MenuSelectionCount, Is.EqualTo(1));
            Assert.That(fixture.EventPublisher.LastMenuItem.HasValue, Is.True);
            Assert.That(fixture.EventPublisher.LastMenuItem.Value.Id, Is.EqualTo(selected.Id));
        }

        private sealed class Fixture : IDisposable
        {
            private readonly StubFoodViewModel _foodViewModel = new();
            private readonly StubFoodEatingService _foodEatingService = new();

            public Fixture(FoodCatalogItem selected = null)
            {
                CommandBus = new RecordingGameCommandBus();
                Window = new RecordingStandaloneWindowViewModel();
                EventPublisher = new RecordingGameEventPublisher();
                Placement = new RecordingFoodPlacementService();
                Handler = new GameCommandHandler(
                    CommandBus,
                    _foodViewModel,
                    Window,
                    EventPublisher,
                    new StubLocalFoodSelectionProvider(selected),
                    Placement,
                    _foodEatingService);
            }

            public RecordingGameCommandBus CommandBus { get; }
            public RecordingStandaloneWindowViewModel Window { get; }
            public RecordingGameEventPublisher EventPublisher { get; }
            public RecordingFoodPlacementService Placement { get; }
            public GameCommandHandler Handler { get; }

            public void Dispose()
            {
                Handler.Dispose();
                CommandBus.Dispose();
                Window.Dispose();
                Placement.Dispose();
                _foodViewModel.Dispose();
                _foodEatingService.Dispose();
            }
        }

        private sealed class RecordingGameCommandBus : IGameCommandBus, IDisposable
        {
            private readonly Subject<GameCommandId> _commands = new();

            public Observable<GameCommandId> OnCommand => _commands;

            public void Request(GameCommandId command) => _commands.OnNext(command);

            public void Dispose() => _commands.Dispose();
        }

        private sealed class RecordingStandaloneWindowViewModel : IStandaloneWindowViewModel, IDisposable
        {
            public ReactiveProperty<bool> IsVisible { get; } = new(false);
            public List<bool> VisibilityChanges { get; } = new();

            public event Action<LocalFoods> OnLocalFoodSpawned
            {
                add { }
                remove { }
            }

            public void SetVisible(bool isVisible)
            {
                IsVisible.Value = isVisible;
                VisibilityChanges.Add(isVisible);
            }

            public void SpawnLocalFood(LocalFoods food)
            {
            }

            public void Dispose() => IsVisible.Dispose();
        }

        private sealed class RecordingGameEventPublisher : IGameEventPublisher
        {
            public int MenuSelectionCount { get; private set; }
            public MenuItem? LastMenuItem { get; private set; }

            public void PublishFoodScooped()
            {
            }

            public void PublishDishCleared()
            {
            }

            public void PublishMenuItemSelected(MenuItem item)
            {
                MenuSelectionCount++;
                LastMenuItem = item;
            }

            public void PublishUserAbsent()
            {
            }

            public void ResetSessionState()
            {
            }
        }

        private sealed class StubLocalFoodSelectionProvider : ILocalFoodSelectionProvider
        {
            private readonly FoodCatalogItem _selected;

            public StubLocalFoodSelectionProvider(FoodCatalogItem selected)
            {
                _selected = selected;
            }

            public bool TryGetSelected(out FoodCatalogItem item)
            {
                item = _selected;
                return item != null;
            }
        }

        private sealed class RecordingFoodPlacementService : IFoodPlacementService, IDisposable
        {
            public ReactiveProperty<Transform> FoodTransform { get; } = new();
            public ReactiveProperty<FoodPlacementState> State { get; } = new();
            public ReactiveProperty<string> StatusMessage { get; } = new();
            public ReactiveProperty<bool> IsAnchorReady { get; } = new();
            public ReactiveProperty<bool> IsFoodPositionFixed { get; } = new();
            public ReactiveProperty<bool> IsConfigurationVisible { get; } = new();
            public ReactiveProperty<bool> IsBusy { get; } = new();
            public ReactiveProperty<bool> IsPlacementConfigured { get; } = new();
            public int ActivationCount { get; private set; }

            public void SetConfigurationVisible(bool isVisible)
            {
            }

            public void UpdateDraftPose(Pose pose)
            {
            }

            public bool TryActivateDraftPoseForFood()
            {
                ActivationCount++;
                return true;
            }

            public bool TryGetSuggestedDraftPose(out Pose pose)
            {
                pose = default;
                return false;
            }

            public UniTask<bool> SetAnchorAtDraftAsync(CancellationToken cancellationToken) =>
                UniTask.FromResult(false);

            public UniTask<bool> FixFoodPositionAtDraftAsync(CancellationToken cancellationToken) =>
                UniTask.FromResult(false);

            public void Dispose()
            {
                FoodTransform.Dispose();
                State.Dispose();
                StatusMessage.Dispose();
                IsAnchorReady.Dispose();
                IsFoodPositionFixed.Dispose();
                IsConfigurationVisible.Dispose();
                IsBusy.Dispose();
                IsPlacementConfigured.Dispose();
            }
        }

        private sealed class StubFoodViewModel : IFoodViewModel, IDisposable
        {
            public ReactiveProperty<GltfImport> foodGltf { get; } = new();
            public ReactiveProperty<AudioClip> chewSound { get; } = new();
            public ReactiveProperty<Transform> foodTransform { get; } = new();
            public ReactiveProperty<float> foodScale { get; } = new();

            private readonly ReactiveProperty<bool> _isPreparing = new(false);
            public ReadOnlyReactiveProperty<bool> isPreparing => _isPreparing;

            public event Action OnFoodResetRequested
            {
                add { }
                remove { }
            }

            public void ResetFoodState()
            {
            }

            public void Dispose()
            {
                foodGltf.Dispose();
                chewSound.Dispose();
                foodTransform.Dispose();
                foodScale.Dispose();
                _isPreparing.Dispose();
            }
        }

        private sealed class StubFoodEatingService : IFoodEatingService, IDisposable
        {
            private readonly ReactiveProperty<float> _remainingFraction = new(1f);
            private readonly ReactiveProperty<bool> _isInteractable = new(false);

            public int TotalPortions => 1;
            public ReadOnlyReactiveProperty<float> RemainingFraction => _remainingFraction;
            public ReadOnlyReactiveProperty<bool> IsInteractable => _isInteractable;

            public void BeginFood()
            {
            }

            public void AbandonFood()
            {
            }

            public bool TryScoop() => false;

            public bool ForceClear() => false;

            public void Dispose()
            {
                _remainingFraction.Dispose();
                _isInteractable.Dispose();
            }
        }
    }
}
