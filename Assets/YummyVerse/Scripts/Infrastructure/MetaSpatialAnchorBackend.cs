using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// Meta XR SDK の Spatial Anchor API をアプリケーション層から隔離する境界。
    /// アンカーの Transform はランタイムが更新するため、配置プレビューモデルとは必ず別 GameObject にする。
    /// </summary>
    public sealed class MetaSpatialAnchorBackend : ISpatialAnchorBackend, IDisposable
    {
        private OVRSpatialAnchor _currentAnchor;
        private OVRSpatialAnchor _previousAnchor;
        private bool _replacementPending;

        public Guid CurrentUuid => _currentAnchor != null && _currentAnchor.Created
            ? _currentAnchor.Uuid
            : Guid.Empty;

        public Transform CurrentAnchorTransform => _currentAnchor != null
            ? _currentAnchor.transform
            : null;

        public async UniTask<SpatialAnchorBackendResult> LoadAsync(Guid uuid, CancellationToken cancellationToken)
        {
            if (uuid == Guid.Empty)
            {
                return SpatialAnchorBackendResult.Failed("Saved Spatial Anchor UUID is invalid.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
                var loadResult = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(
                    new[] { uuid },
                    unboundAnchors);

                cancellationToken.ThrowIfCancellationRequested();
                if (!loadResult.Success || unboundAnchors.Count == 0)
                {
                    return SpatialAnchorBackendResult.Failed(
                        $"Could not load Spatial Anchor ({loadResult.Status}).");
                }

                var unboundAnchor = unboundAnchors[0];
                if (!unboundAnchor.Localized && !await unboundAnchor.LocalizeAsync(10))
                {
                    return SpatialAnchorBackendResult.Failed("Could not localize the saved Spatial Anchor.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!unboundAnchor.TryGetPose(out var pose))
                {
                    return SpatialAnchorBackendResult.Failed("The saved Spatial Anchor pose is unavailable.");
                }

                var anchorObject = new GameObject("YummyVerse Spatial Anchor");
                anchorObject.transform.SetPositionAndRotation(pose.position, pose.rotation);

                // BindTo は AddComponent と同じフレーム、かつ Start より前に行う必要がある。
                var anchor = anchorObject.AddComponent<OVRSpatialAnchor>();
                unboundAnchor.BindTo(anchor);
                ReplaceCurrentReference(anchor);

                return SpatialAnchorBackendResult.Succeeded(anchor.Uuid, anchor.transform);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return SpatialAnchorBackendResult.Failed($"Spatial Anchor load failed: {exception.Message}");
            }
        }

        public async UniTask<SpatialAnchorBackendResult> ReplaceAsync(Pose pose, CancellationToken cancellationToken)
        {
            OVRSpatialAnchor newAnchor = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var anchorObject = new GameObject("YummyVerse Spatial Anchor");
                anchorObject.transform.SetPositionAndRotation(pose.position, pose.rotation);
                newAnchor = anchorObject.AddComponent<OVRSpatialAnchor>();

                if (!await newAnchor.WhenLocalizedAsync())
                {
                    DestroyAnchorObject(newAnchor);
                    return SpatialAnchorBackendResult.Failed("Could not create or localize the Spatial Anchor.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                var saveResult = await newAnchor.SaveAnchorAsync();
                if (!saveResult.Success)
                {
                    var message = saveResult.Status == OVRAnchor.SaveResult.FailureInsufficientView
                        ? "Spatial Anchor needs a clearer view of the surroundings. Look around and retry."
                        : $"Could not save Spatial Anchor ({saveResult.Status}).";
                    DestroyAnchorObject(newAnchor);
                    return SpatialAnchorBackendResult.Failed(message);
                }

                if (_replacementPending)
                {
                    // 固定前に再び設定された場合は、さらに古い active Anchor は保持し、
                    // 直前の未確定 Anchor だけを破棄する。
                    await EraseAndDestroyAsync(_currentAnchor, "Superseded draft");
                }
                else
                {
                    _previousAnchor = _currentAnchor;
                    _replacementPending = true;
                }

                _currentAnchor = newAnchor;

                return SpatialAnchorBackendResult.Succeeded(newAnchor.Uuid, newAnchor.transform);
            }
            catch (OperationCanceledException)
            {
                if (newAnchor != null && newAnchor != _currentAnchor)
                {
                    DestroyAnchorObject(newAnchor);
                }
                throw;
            }
            catch (Exception exception)
            {
                if (newAnchor != null && newAnchor != _currentAnchor)
                {
                    DestroyAnchorObject(newAnchor);
                }
                Debug.LogException(exception);
                return SpatialAnchorBackendResult.Failed($"Spatial Anchor save failed: {exception.Message}");
            }
        }

        public async UniTask CommitReplacementAsync()
        {
            if (!_replacementPending) return;

            var previousAnchor = _previousAnchor;
            _previousAnchor = null;
            _replacementPending = false;
            await EraseAndDestroyAsync(previousAnchor, "Old");
        }

        public async UniTask RollbackReplacementAsync()
        {
            if (!_replacementPending) return;

            var rejectedAnchor = _currentAnchor;
            _currentAnchor = _previousAnchor;
            _previousAnchor = null;
            _replacementPending = false;
            await EraseAndDestroyAsync(rejectedAnchor, "Rejected draft");
        }

        public void Dispose()
        {
            // 固定前の draft Anchor は永続レコードから参照されないため、終了時にも消去を試みる。
            if (_replacementPending)
            {
                var abandonedDraft = _currentAnchor;
                _currentAnchor = _previousAnchor;
                _previousAnchor = null;
                _replacementPending = false;
                EraseAndDestroyAsync(abandonedDraft, "Abandoned draft").Forget();
            }

            // 確定済みアンカー自体は展示設定なので消去しない。シーン上の表現だけ破棄する。
            DestroyAnchorObject(_currentAnchor);
            DestroyAnchorObject(_previousAnchor);
            _currentAnchor = null;
            _previousAnchor = null;
            _replacementPending = false;
        }

        private void ReplaceCurrentReference(OVRSpatialAnchor anchor)
        {
            if (_currentAnchor != null && _currentAnchor != anchor)
            {
                DestroyAnchorObject(_currentAnchor);
            }
            _currentAnchor = anchor;
        }

        private static async UniTask EraseAndDestroyAsync(OVRSpatialAnchor anchor, string label)
        {
            if (anchor == null) return;

            try
            {
                var eraseResult = await anchor.EraseAnchorAsync();
                if (!eraseResult.Success)
                {
                    Debug.LogWarning(
                        $"[FoodPlacement] {label} Spatial Anchor could not be erased ({eraseResult.Status}).");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[FoodPlacement] {label} Spatial Anchor cleanup failed: {exception.Message}");
            }
            DestroyAnchorObject(anchor);
        }

        private static void DestroyAnchorObject(OVRSpatialAnchor anchor)
        {
            if (anchor != null)
            {
                UnityEngine.Object.Destroy(anchor.gameObject);
            }
        }
    }
}
