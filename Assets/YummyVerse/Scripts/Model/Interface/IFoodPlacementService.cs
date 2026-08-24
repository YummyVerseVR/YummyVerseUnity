using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodPlacementService
    {
        ReactiveProperty<Transform> FoodTransform { get; }
        ReactiveProperty<FoodPlacementState> State { get; }
        ReactiveProperty<string> StatusMessage { get; }
        ReactiveProperty<bool> IsAnchorReady { get; }
        ReactiveProperty<bool> IsFoodPositionFixed { get; }
        ReactiveProperty<bool> IsConfigurationVisible { get; }
        ReactiveProperty<bool> IsBusy { get; }

        void SetConfigurationVisible(bool isVisible);
        void UpdateDraftPose(Pose pose);
        bool TryActivateDraftPoseForFood();
        bool TryGetSuggestedDraftPose(out Pose pose);
        UniTask<bool> SetAnchorAtDraftAsync(CancellationToken cancellationToken);
        UniTask<bool> FixFoodPositionAtDraftAsync(CancellationToken cancellationToken);
    }
}
