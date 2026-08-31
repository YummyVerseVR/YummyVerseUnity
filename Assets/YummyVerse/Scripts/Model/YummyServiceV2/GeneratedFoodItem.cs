using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace YummyVerse.Scripts.Model.YummyServiceV2
{
    /// <summary>
    /// Application-domain representation of a generated order/menu item.
    ///
    /// It stores all five stage states and only the selected immutable artifact
    /// revisions. It never derives a selected revision from a latest/current artifact;
    /// selection must be explicit at the v2 boundary.
    /// </summary>
    public sealed class GeneratedFoodItem
    {
        private readonly IReadOnlyDictionary<StageType, StageState> _stageStates;
        private readonly IReadOnlyDictionary<ArtifactType, ArtifactRef> _selectedArtifacts;

        public GeneratedFoodItem(
            GeneratedFoodItemId id,
            OrderState orderState,
            IEnumerable<KeyValuePair<StageType, StageState>> stageStates,
            IEnumerable<KeyValuePair<ArtifactType, ArtifactRef>> selectedArtifacts)
        {
            Id = id;
            OrderState = orderState;
            _stageStates = CopyStageStates(stageStates);
            _selectedArtifacts = CopySelectedArtifacts(selectedArtifacts);
        }

        public GeneratedFoodItemId Id { get; }
        public string OrderIdentity => Id.OrderIdentity;
        public OrderState OrderState { get; }
        public IReadOnlyDictionary<StageType, StageState> StageStates => _stageStates;
        public IReadOnlyDictionary<ArtifactType, ArtifactRef> SelectedArtifacts => _selectedArtifacts;

        public bool HasAllStageStates
        {
            get
            {
                foreach (var stageType in KnownStageTypes)
                {
                    if (!_stageStates.ContainsKey(stageType) || _stageStates[stageType] == StageState.Unknown)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsValid
        {
            get
            {
                if (!Id.IsValid || !Enum.IsDefined(typeof(OrderState), OrderState) || OrderState == OrderState.Unknown)
                {
                    return false;
                }

                if (!HasSemanticallyValidStageStates())
                {
                    return false;
                }

                foreach (var selected in _selectedArtifacts)
                {
                    if (selected.Key == ArtifactType.Unknown
                        || !Enum.IsDefined(typeof(ArtifactType), selected.Key)
                        || selected.Value.ArtifactType != selected.Key
                        || !selected.Value.IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsSelectable => IsSelectableForMenu();

        public bool IsImageTo3DBranchIndependent =>
            GetStageState(StageType.ImageTo3D) == StageState.Pending
            || GetStageState(StageType.ImageTo3D) == StageState.Processing
            || GetStageState(StageType.ImageTo3D) == StageState.Queued
            || GetStageState(StageType.ImageTo3D) == StageState.Completed;

        public bool TryGetStageState(StageType stageType, out StageState state)
        {
            return _stageStates.TryGetValue(stageType, out state) && stageType != StageType.Unknown;
        }

        public StageState GetStageState(StageType stageType)
        {
            return TryGetStageState(stageType, out var state) ? state : StageState.Unknown;
        }

        public bool TryGetSelectedArtifact(ArtifactType artifactType, out ArtifactRef artifact)
        {
            return _selectedArtifacts.TryGetValue(artifactType, out artifact) && artifactType != ArtifactType.Unknown;
        }

        public bool TryGetSelectedGlb(out ArtifactRef artifact)
        {
            return TryGetSelectedArtifact(ArtifactType.Glb, out artifact);
        }

        /// <summary>
        /// 咀嚼音に使う selected WAV。
        /// Device の status projection では <c>wav.downloadable</c> が true のときだけ
        /// <c>artifact_id</c> が返るため、false のときにここへ artifact を詰めてはならない。
        /// </summary>
        public bool TryGetSelectedWav(out ArtifactRef artifact)
        {
            return TryGetSelectedArtifact(ArtifactType.Wav, out artifact);
        }

        /// <summary>
        /// The minimum ready gate from the consumer contract. This overload is useful
        /// at an adapter boundary before the complete order snapshot is assembled.
        /// </summary>
        public static bool MeetsMinimumReadyGate(OrderState orderState, ArtifactRef? selectedGlb)
        {
            return orderState == OrderState.Completed
                   && selectedGlb.HasValue
                   && selectedGlb.Value.IsVerifiedGlb;
        }

        private bool IsSelectableForMenu()
        {
            if (!IsValid || !MeetsMinimumReadyGate(OrderState, GetSelectedGlb()))
            {
                return false;
            }

            // A completed order is expected to satisfy the v2 completion gate. Keeping
            // this check local prevents malformed/missing stage responses from becoming
            // menu-ready through an optimistic order-level state alone.
            return GetStageState(StageType.InputModeration) == StageState.Completed
                   && (GetStageState(StageType.ExampleRetrieval) == StageState.Completed
                       || GetStageState(StageType.ExampleRetrieval) == StageState.CompletedWithWarning)
                   && GetStageState(StageType.FoodAnalysis) == StageState.Completed
                   && GetStageState(StageType.ImageTo3D) == StageState.Completed
                   && GetStageState(StageType.AudioGeneration) == StageState.Completed;
        }

        private ArtifactRef? GetSelectedGlb()
        {
            return TryGetSelectedGlb(out var artifact) ? artifact : (ArtifactRef?)null;
        }

        private bool HasSemanticallyValidStageStates()
        {
            if (!HasAllStageStates)
            {
                return false;
            }

            foreach (var stage in _stageStates)
            {
                if (!Enum.IsDefined(typeof(StageType), stage.Key)
                    || stage.Key == StageType.Unknown
                    || !Enum.IsDefined(typeof(StageState), stage.Value)
                    || stage.Value == StageState.Unknown)
                {
                    return false;
                }

                // Retrieval exhaustion/Zero Shot is the only confirmed warning outcome.
                if (stage.Value == StageState.CompletedWithWarning && stage.Key != StageType.ExampleRetrieval)
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyDictionary<StageType, StageState> CopyStageStates(
            IEnumerable<KeyValuePair<StageType, StageState>> source)
        {
            var copy = new Dictionary<StageType, StageState>();
            if (source != null)
            {
                foreach (var entry in source)
                {
                    copy[entry.Key] = entry.Value;
                }
            }

            return new ReadOnlyDictionary<StageType, StageState>(copy);
        }

        private static IReadOnlyDictionary<ArtifactType, ArtifactRef> CopySelectedArtifacts(
            IEnumerable<KeyValuePair<ArtifactType, ArtifactRef>> source)
        {
            var copy = new Dictionary<ArtifactType, ArtifactRef>();
            if (source != null)
            {
                foreach (var entry in source)
                {
                    copy[entry.Key] = entry.Value;
                }
            }

            return new ReadOnlyDictionary<ArtifactType, ArtifactRef>(copy);
        }

        private static readonly StageType[] KnownStageTypes =
        {
            StageType.InputModeration,
            StageType.ExampleRetrieval,
            StageType.FoodAnalysis,
            StageType.ImageTo3D,
            StageType.AudioGeneration
        };
    }

}
