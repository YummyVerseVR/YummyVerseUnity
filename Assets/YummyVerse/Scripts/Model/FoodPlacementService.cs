using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    public sealed class FoodPlacementService : IFoodPlacementService, IInitializable, IDisposable
    {
        private readonly ISpatialAnchorBackend _spatialAnchorBackend;
        private readonly IFoodPlacementStore _placementStore;
        private readonly CancellationTokenSource _lifetimeCancellation = new();

        private FoodPlacementData _data;
        private Transform _foodPlacementRoot;
        private Pose _draftPose;
        private bool _hasDraftPose;

        public ReactiveProperty<Transform> FoodTransform { get; } = new();
        public ReactiveProperty<FoodPlacementState> State { get; } = new(FoodPlacementState.Unconfigured);
        public ReactiveProperty<string> StatusMessage { get; } = new("Spatial Anchor is not configured.");
        public ReactiveProperty<bool> IsAnchorReady { get; } = new(false);
        public ReactiveProperty<bool> IsFoodPositionFixed { get; } = new(false);
        public ReactiveProperty<bool> IsConfigurationVisible { get; } = new(false);
        public ReactiveProperty<bool> IsBusy { get; } = new(false);

        public FoodPlacementService(
            ISpatialAnchorBackend spatialAnchorBackend,
            IFoodPlacementStore placementStore)
        {
            _spatialAnchorBackend = spatialAnchorBackend;
            _placementStore = placementStore;
        }

        public void Initialize()
        {
            RestoreAsync(_lifetimeCancellation.Token).Forget();
        }

        public void SetConfigurationVisible(bool isVisible)
        {
            IsConfigurationVisible.Value = isVisible;

            if (isVisible && !IsBusy.Value)
            {
                State.Value = FoodPlacementState.Editing;
                StatusMessage.Value = IsAnchorReady.Value
                    ? "Move or rotate the food model, then lock its position."
                    : "Move or rotate the food model, then set the Spatial Anchor.";
            }
            else if (!isVisible && !IsBusy.Value && IsFoodPositionFixed.Value)
            {
                State.Value = FoodPlacementState.Ready;
                StatusMessage.Value = "Food position is fixed to the Spatial Anchor.";
            }
        }

        public void UpdateDraftPose(Pose pose)
        {
            _draftPose = pose;
            _hasDraftPose = true;
        }

        /// <summary>
        /// 設定用モデルの最新world poseを、次に表示する食品の基準Transformへ反映する。
        /// 永続化前でもチュートリアル食品をプレビューと同じ場所・回転に出せる。
        /// </summary>
        public bool TryActivateDraftPoseForFood()
        {
            if (!_hasDraftPose) return FoodTransform.Value != null;

            if (_foodPlacementRoot == null)
            {
                _foodPlacementRoot = new GameObject("Food Placement Root").transform;
            }

            var anchor = _spatialAnchorBackend.CurrentAnchorTransform;
            _foodPlacementRoot.SetParent(anchor, true);
            _foodPlacementRoot.SetPositionAndRotation(
                _draftPose.position,
                NormalizeRotation(_draftPose.rotation));

            var previous = FoodTransform.Value;
            FoodTransform.Value = _foodPlacementRoot;
            if (ReferenceEquals(previous, _foodPlacementRoot))
            {
                FoodTransform.OnNext(_foodPlacementRoot);
            }
            return true;
        }

        public bool TryGetSuggestedDraftPose(out Pose pose)
        {
            if (_hasDraftPose)
            {
                pose = _draftPose;
                return true;
            }

            if (FoodTransform.Value != null)
            {
                pose = new Pose(FoodTransform.Value.position, FoodTransform.Value.rotation);
                return true;
            }

            if (_spatialAnchorBackend.CurrentAnchorTransform != null)
            {
                var anchor = _spatialAnchorBackend.CurrentAnchorTransform;
                pose = new Pose(anchor.position, anchor.rotation);
                return true;
            }

            pose = default;
            return false;
        }

        public async UniTask<bool> SetAnchorAtDraftAsync(CancellationToken cancellationToken)
        {
            if (!_hasDraftPose || IsBusy.Value) return false;

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token);

            IsBusy.Value = true;
            State.Value = FoodPlacementState.Saving;
            StatusMessage.Value = "Saving Spatial Anchor...";

            try
            {
                var result = await _spatialAnchorBackend.ReplaceAsync(_draftPose, linkedCancellation.Token);
                if (!result.Success)
                {
                    State.Value = FoodPlacementState.Error;
                    StatusMessage.Value = result.ErrorMessage;
                    return false;
                }

                IsAnchorReady.Value = true;
                IsFoodPositionFixed.Value = false;
                State.Value = FoodPlacementState.Editing;
                StatusMessage.Value = "Anchor saved. Move or rotate the food model, then lock its position.";
                return true;
            }
            catch (OperationCanceledException)
            {
                State.Value = FoodPlacementState.Error;
                StatusMessage.Value = "Spatial Anchor setup was canceled. Open settings to retry.";
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                State.Value = FoodPlacementState.Error;
                StatusMessage.Value = $"Spatial Anchor setup failed: {exception.Message}";
                return false;
            }
            finally
            {
                IsBusy.Value = false;
            }
        }

        public async UniTask<bool> FixFoodPositionAtDraftAsync(CancellationToken cancellationToken)
        {
            var anchor = _spatialAnchorBackend.CurrentAnchorTransform;
            if (!_hasDraftPose || anchor == null || IsBusy.Value)
            {
                StatusMessage.Value = "Set the Spatial Anchor before locking the food position.";
                return false;
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token);

            IsBusy.Value = true;
            State.Value = FoodPlacementState.Saving;
            StatusMessage.Value = "Saving food position...";

            var newData = new FoodPlacementData
            {
                SchemaVersion = FoodPlacementData.CurrentSchemaVersion,
                AnchorUuid = _spatialAnchorBackend.CurrentUuid.ToString("D"),
                HasFoodPose = true,
                LocalPosition = anchor.InverseTransformPoint(_draftPose.position),
                LocalRotation = Quaternion.Inverse(anchor.rotation) * _draftPose.rotation
            };

            try
            {
                linkedCancellation.Token.ThrowIfCancellationRequested();
                _placementStore.Save(newData);
                _data = newData;
                ApplyFoodTransform(anchor, _data);
                await _spatialAnchorBackend.CommitReplacementAsync();
                IsAnchorReady.Value = true;
                IsFoodPositionFixed.Value = true;
                State.Value = FoodPlacementState.Ready;
                StatusMessage.Value = "Food position is fixed to the Spatial Anchor.";
                return true;
            }
            catch (OperationCanceledException)
            {
                await _spatialAnchorBackend.RollbackReplacementAsync();
                RestoreStatusAfterFailedCommit("Food position was not changed.");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                await _spatialAnchorBackend.RollbackReplacementAsync();
                RestoreStatusAfterFailedCommit($"Could not save the food position: {exception.Message}");
                return false;
            }
            finally
            {
                IsBusy.Value = false;
            }
        }

        public void Dispose()
        {
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
            ClearFoodTransform();

            FoodTransform.Dispose();
            State.Dispose();
            StatusMessage.Dispose();
            IsAnchorReady.Dispose();
            IsFoodPositionFixed.Dispose();
            IsConfigurationVisible.Dispose();
            IsBusy.Dispose();
        }

        private async UniTask RestoreAsync(CancellationToken cancellationToken)
        {
            if (!_placementStore.TryLoad(out _data) || !_data.TryGetAnchorUuid(out var uuid))
            {
                State.Value = FoodPlacementState.Unconfigured;
                StatusMessage.Value = "Spatial Anchor is not configured.";
                return;
            }

            IsBusy.Value = true;
            State.Value = FoodPlacementState.Loading;
            StatusMessage.Value = "Loading saved Spatial Anchor...";

            try
            {
                var result = await _spatialAnchorBackend.LoadAsync(uuid, cancellationToken);
                if (!result.Success)
                {
                    Debug.LogWarning($"[FoodPlacement] {result.ErrorMessage}");
                    State.Value = FoodPlacementState.Error;
                    StatusMessage.Value = $"{result.ErrorMessage} Open settings to configure it again.";
                    return;
                }

                IsAnchorReady.Value = true;
                if (!_data.HasFoodPose)
                {
                    State.Value = FoodPlacementState.Editing;
                    StatusMessage.Value = "Move or rotate the food model, then lock its position.";
                    return;
                }

                ApplyFoodTransform(result.AnchorTransform, _data);
                IsFoodPositionFixed.Value = true;
                if (IsConfigurationVisible.Value)
                {
                    State.Value = FoodPlacementState.Editing;
                    StatusMessage.Value = "Move or rotate the food model, then lock its position.";
                }
                else
                {
                    State.Value = FoodPlacementState.Ready;
                    StatusMessage.Value = "Food position restored from Spatial Anchor.";
                }
            }
            catch (OperationCanceledException)
            {
                // Scene disposalによるキャンセルはエラーとして表示しない。
            }
            finally
            {
                IsBusy.Value = false;
            }
        }

        private void ApplyFoodTransform(Transform anchor, FoodPlacementData data)
        {
            if (_foodPlacementRoot == null)
            {
                _foodPlacementRoot = new GameObject("Food Placement Root").transform;
            }

            _foodPlacementRoot.SetParent(anchor, false);
            _foodPlacementRoot.localPosition = data.LocalPosition;
            _foodPlacementRoot.localRotation = NormalizeRotation(data.LocalRotation);

            var previous = FoodTransform.Value;
            FoodTransform.Value = _foodPlacementRoot;
            if (ReferenceEquals(previous, _foodPlacementRoot))
            {
                FoodTransform.OnNext(_foodPlacementRoot);
            }
        }

        private void ClearFoodTransform()
        {
            FoodTransform.Value = null;
            if (_foodPlacementRoot != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_foodPlacementRoot.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_foodPlacementRoot.gameObject);
                }
                _foodPlacementRoot = null;
            }
        }

        private void RestoreStatusAfterFailedCommit(string message)
        {
            var hasPreviousPlacement = FoodTransform.Value != null;
            IsAnchorReady.Value = _spatialAnchorBackend.CurrentAnchorTransform != null;
            IsFoodPositionFixed.Value = hasPreviousPlacement;
            State.Value = FoodPlacementState.Error;
            StatusMessage.Value = hasPreviousPlacement
                ? $"{message} The previous placement is still active."
                : message;
        }

        private static Quaternion NormalizeRotation(Quaternion rotation)
        {
            return rotation == default ? Quaternion.identity : Quaternion.Normalize(rotation);
        }
    }
}
