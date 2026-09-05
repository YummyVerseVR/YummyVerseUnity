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
    /// <summary>
    /// 食品をどこに出すかを決め、Spatial Anchor に貼り付けたまま保つ。
    ///
    /// 置き場所は必ず「アンカー基準 (local)」で持つ。ワールド姿勢で持ってはいけない。
    /// HMD を被り直すとランタイムはトラッキング原点を取り直すため、ワールド座標は
    /// 物理空間に対して動く。アンカーの Transform はランタイムが毎フレーム
    /// 物理空間に合わせて更新してくれるので、そこからの相対で持っている限りだけ、
    /// 食品は現実の同じ場所に居続ける。
    /// </summary>
    public sealed class FoodPlacementService : IFoodPlacementService, IInitializable, ITickable, IDisposable
    {
        private const int AnchorLoadAttempts = 3;
        private const float AnchorLoadRetryDelaySeconds = 1.5f;

        private readonly ISpatialAnchorBackend _spatialAnchorBackend;
        private readonly IFoodPlacementStore _placementStore;
        private readonly CancellationTokenSource _lifetimeCancellation = new();

        private FoodPlacementData _data;
        private Transform _foodPlacementRoot;

        /// <summary>設定画面の配置モデルの姿勢。アンカーがある間はアンカー基準で持つ。</summary>
        private Pose _draftPose;
        private bool _hasDraftPose;
        private bool _isDraftAnchorRelative;

        /// <summary>いま食品を出している置き場所の姿勢。意味は <see cref="_draftPose"/> と同じ。</summary>
        private Pose _activePose;
        private bool _hasActivePose;
        private bool _isActiveAnchorRelative;

        public ReactiveProperty<Transform> FoodTransform { get; } = new();
        public ReactiveProperty<FoodPlacementState> State { get; } = new(FoodPlacementState.Unconfigured);
        public ReactiveProperty<string> StatusMessage { get; } = new("Spatial Anchor is not configured.");
        public ReactiveProperty<bool> IsAnchorReady { get; } = new(false);
        public ReactiveProperty<bool> IsFoodPositionFixed { get; } = new(false);
        public ReactiveProperty<bool> IsConfigurationVisible { get; } = new(false);
        public ReactiveProperty<bool> IsBusy { get; } = new(false);
        public ReactiveProperty<bool> IsPlacementConfigured { get; } = new(false);

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

        /// <summary>
        /// 置き場所を現在のアンカーに繋ぎ直し続ける。
        /// アンカーは設定のやり直しや復元完了で GameObject ごと作り替わる。
        /// 繋ぎ直しを怠ると、食品は「作られた当時のワールド座標」に取り残され、
        /// 被り直しのたびに物理空間からずれていく。
        /// </summary>
        public void Tick()
        {
            if (!_hasActivePose) return;

            if (_foodPlacementRoot == null)
            {
                // 親アンカーごと破棄された場合はここに来る。現在のアンカーの下に作り直す。
                ApplyPlacement();
                return;
            }

            var anchor = _spatialAnchorBackend.CurrentAnchorTransform;
            if (ReferenceEquals(_foodPlacementRoot.parent, anchor)) return;

            ApplyPlacement();
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

        /// <summary>
        /// 設定用モデルのワールド姿勢を受け取る。受け取った時点でアンカー基準へ直す。
        /// 「後で使うときに直す」では間に合わない。使うのは何分も後で、
        /// その間に被り直しが挟まればワールド座標の意味が変わってしまう。
        /// </summary>
        public void UpdateDraftPose(Pose pose)
        {
            var anchor = _spatialAnchorBackend.CurrentAnchorTransform;
            _isDraftAnchorRelative = anchor != null;
            _draftPose = _isDraftAnchorRelative ? ToAnchorLocalPose(anchor, pose) : pose;
            _hasDraftPose = true;
            RefreshPlacementConfigured();
        }

        /// <summary>
        /// 設定用モデルの最新の姿勢を、次に表示する食品の基準Transformへ反映する。
        /// 永続化前でもチュートリアル食品をプレビューと同じ場所・回転に出せる。
        /// </summary>
        public bool TryActivateDraftPoseForFood()
        {
            if (_hasDraftPose)
            {
                _activePose = _draftPose;
                _isActiveAnchorRelative = _isDraftAnchorRelative;
                _hasActivePose = true;
            }

            if (!_hasActivePose) return false;

            ApplyPlacement();
            return FoodTransform.Value != null;
        }

        public bool TryGetSuggestedDraftPose(out Pose pose)
        {
            var anchor = _spatialAnchorBackend.CurrentAnchorTransform;

            if (_hasDraftPose && TryResolveWorldPose(_draftPose, _isDraftAnchorRelative, anchor, out pose))
            {
                return true;
            }

            if (_hasActivePose && TryResolveWorldPose(_activePose, _isActiveAnchorRelative, anchor, out pose))
            {
                return true;
            }

            if (anchor != null)
            {
                pose = new Pose(anchor.position, anchor.rotation);
                return true;
            }

            pose = default;
            return false;
        }

        public async UniTask<bool> SetAnchorAtDraftAsync(CancellationToken cancellationToken)
        {
            if (!_hasDraftPose || IsBusy.Value) return false;

            var currentAnchor = _spatialAnchorBackend.CurrentAnchorTransform;
            if (!TryResolveWorldPose(_draftPose, _isDraftAnchorRelative, currentAnchor, out var draftWorldPose))
            {
                State.Value = FoodPlacementState.Error;
                StatusMessage.Value = "The placement pose could not be resolved. Move the food model and retry.";
                return false;
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token);

            IsBusy.Value = true;
            State.Value = FoodPlacementState.Saving;
            StatusMessage.Value = "Saving Spatial Anchor...";

            try
            {
                var result = await _spatialAnchorBackend.ReplaceAsync(draftWorldPose, linkedCancellation.Token);
                if (!result.Success)
                {
                    State.Value = FoodPlacementState.Error;
                    StatusMessage.Value = result.ErrorMessage;
                    return false;
                }

                // 新しいアンカーができた。下書きをその基準へ移し替えておく。
                RebaseDraftOnCurrentAnchor(draftWorldPose);

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

            var localPose = _isDraftAnchorRelative
                ? _draftPose
                : ToAnchorLocalPose(anchor, _draftPose);

            var newData = new FoodPlacementData
            {
                SchemaVersion = FoodPlacementData.CurrentSchemaVersion,
                AnchorUuid = _spatialAnchorBackend.CurrentUuid.ToString("D"),
                HasFoodPose = true,
                LocalPosition = localPose.position,
                LocalRotation = NormalizeRotation(localPose.rotation)
            };

            try
            {
                linkedCancellation.Token.ThrowIfCancellationRequested();
                _placementStore.Save(newData);
                _data = newData;

                _draftPose = localPose;
                _isDraftAnchorRelative = true;
                _activePose = localPose;
                _isActiveAnchorRelative = true;
                _hasActivePose = true;
                ApplyPlacement();

                await _spatialAnchorBackend.CommitReplacementAsync();
                IsAnchorReady.Value = true;
                IsFoodPositionFixed.Value = true;
                State.Value = FoodPlacementState.Ready;
                StatusMessage.Value = "Food position is fixed to the Spatial Anchor.";
                Debug.Log(
                    $"[FoodPlacement] 食品位置を Spatial Anchor {newData.AnchorUuid} に固定しました "
                    + $"(local pos {localPose.position}).");
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
            IsPlacementConfigured.Dispose();
        }

        private async UniTask RestoreAsync(CancellationToken cancellationToken)
        {
            if (!_placementStore.TryLoad(out _data) || !_data.TryGetAnchorUuid(out var uuid))
            {
                State.Value = FoodPlacementState.Unconfigured;
                StatusMessage.Value = "Spatial Anchor is not configured.";
                return;
            }

            // 復元は Spatial Anchor の localize 待ちで数秒かかる。IsBusy を見ている側は
            // この間「表示位置が決まっていない」ではなく「決着していない」として扱うこと。
            IsBusy.Value = true;
            State.Value = FoodPlacementState.Loading;
            StatusMessage.Value = "Loading saved Spatial Anchor...";

            try
            {
                var result = await LoadAnchorWithRetryAsync(uuid, cancellationToken);
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

                _activePose = new Pose(_data.LocalPosition, NormalizeRotation(_data.LocalRotation));
                _isActiveAnchorRelative = true;
                _hasActivePose = true;
                ApplyPlacement();
                Debug.Log(
                    $"[FoodPlacement] Spatial Anchor {_data.AnchorUuid} を復元し、"
                    + $"食品位置を貼り直しました (local pos {_data.LocalPosition}).");

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

        /// <summary>
        /// 起動直後は空間データの読み込みが終わっておらず、保存済みアンカーの localize が
        /// 素直に失敗することがある。ここで一度で諦めると「設定したのに食品が出ない」に見える。
        /// 数回だけ間を空けて粘り、それでも駄目なときにだけ設定し直しを促す。
        /// </summary>
        private async UniTask<SpatialAnchorBackendResult> LoadAnchorWithRetryAsync(
            Guid uuid,
            CancellationToken cancellationToken)
        {
            var result = default(SpatialAnchorBackendResult);

            for (var attempt = 1; attempt <= AnchorLoadAttempts; attempt++)
            {
                result = await _spatialAnchorBackend.LoadAsync(uuid, cancellationToken);
                if (result.Success) return result;

                if (attempt == AnchorLoadAttempts) break;

                // PlayerLoop が回らない実行 (エディタのテストなど) では待てない。粘るのは実行中だけ。
                if (!Application.isPlaying) break;

                Debug.LogWarning(
                    $"[FoodPlacement] Spatial Anchor の読み込みに失敗しました "
                    + $"({attempt}/{AnchorLoadAttempts}): {result.ErrorMessage} 少し待って再試行します。");
                StatusMessage.Value = $"Retrying to load the saved Spatial Anchor ({attempt}/{AnchorLoadAttempts})...";
                await UniTask.Delay(
                    TimeSpan.FromSeconds(AnchorLoadRetryDelaySeconds),
                    cancellationToken: cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// 置き場所 Transform を現在のアンカーの子として置き直す。
        /// アンカーの子にしておけば、ランタイムがアンカーを物理空間へ合わせ直すたびに
        /// 食品も一緒に付いていく。ここでワールド姿勢を焼き付けてはいけない。
        /// </summary>
        private void ApplyPlacement()
        {
            if (!_hasActivePose) return;

            var anchor = _spatialAnchorBackend.CurrentAnchorTransform;

            if (_isActiveAnchorRelative && anchor == null)
            {
                // アンカーがまだ (もう) 無い。ここでローカル姿勢をワールドとして置くと
                // 現実と無関係な場所に食品が出る。解決できるようになるまで作らない。
                return;
            }

            // アンカーが後から用意された場合、ワールドで持っていた置き場所をここで基準へ移す。
            if (anchor != null && !_isActiveAnchorRelative)
            {
                _activePose = ToAnchorLocalPose(anchor, _activePose);
                _isActiveAnchorRelative = true;
            }

            if (_foodPlacementRoot == null)
            {
                _foodPlacementRoot = new GameObject("Food Placement Root").transform;
            }

            if (anchor != null)
            {
                if (!ReferenceEquals(_foodPlacementRoot.parent, anchor))
                {
                    _foodPlacementRoot.SetParent(anchor, false);
                }
                _foodPlacementRoot.localPosition = _activePose.position;
                _foodPlacementRoot.localRotation = NormalizeRotation(_activePose.rotation);
            }
            else
            {
                // アンカー未設定。設定画面で置いた場所をそのまま使う暫定表示で、
                // 被り直しをまたぐと現実に対してずれる。設定を保存してもらうまでの繋ぎ。
                if (_foodPlacementRoot.parent != null)
                {
                    _foodPlacementRoot.SetParent(null, false);
                }
                _foodPlacementRoot.SetPositionAndRotation(
                    _activePose.position,
                    NormalizeRotation(_activePose.rotation));
            }

            PublishFoodTransform();
        }

        private void PublishFoodTransform()
        {
            var previous = FoodTransform.Value;
            FoodTransform.Value = _foodPlacementRoot;
            if (ReferenceEquals(previous, _foodPlacementRoot))
            {
                FoodTransform.OnNext(_foodPlacementRoot);
            }
            RefreshPlacementConfigured();
        }

        private void RebaseDraftOnCurrentAnchor(Pose draftWorldPose)
        {
            var anchor = _spatialAnchorBackend.CurrentAnchorTransform;
            if (anchor == null) return;

            _draftPose = ToAnchorLocalPose(anchor, draftWorldPose);
            _isDraftAnchorRelative = true;
        }

        private void ClearFoodTransform()
        {
            FoodTransform.Value = null;
            _hasActivePose = false;
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
            }
            _foodPlacementRoot = null;
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

        /// <summary>
        /// 「食べ物を出せる表示先が今あるか」を再評価する。
        /// ここが false のまま食べ物を生成すると FoodView が表示先を持てず、
        /// モデルは読み込まれているのに何も見えない状態になる。
        /// 保存済み設定があっても Spatial Anchor の復元に失敗すれば false のままである点に注意。
        /// </summary>
        private void RefreshPlacementConfigured()
        {
            IsPlacementConfigured.Value = _hasDraftPose || FoodTransform.Value != null;
        }

        /// <summary>ワールド姿勢をアンカー基準へ直す。スケールに依存しない素の相対姿勢を使う。</summary>
        private static Pose ToAnchorLocalPose(Transform anchor, Pose worldPose)
        {
            var inverseAnchorRotation = Quaternion.Inverse(anchor.rotation);
            return new Pose(
                inverseAnchorRotation * (worldPose.position - anchor.position),
                NormalizeRotation(inverseAnchorRotation * worldPose.rotation));
        }

        /// <summary>
        /// 保持している姿勢をワールドへ戻す。アンカー基準なのにアンカーが無いときは
        /// 意味のある値を作れないので false を返す。ローカル値をワールドとして使い回さない。
        /// </summary>
        private static bool TryResolveWorldPose(
            Pose pose,
            bool isAnchorRelative,
            Transform anchor,
            out Pose worldPose)
        {
            if (!isAnchorRelative)
            {
                worldPose = pose;
                return true;
            }

            if (anchor == null)
            {
                worldPose = default;
                return false;
            }

            worldPose = new Pose(
                anchor.position + anchor.rotation * pose.position,
                NormalizeRotation(anchor.rotation * pose.rotation));
            return true;
        }

        private static Quaternion NormalizeRotation(Quaternion rotation)
        {
            return rotation == default ? Quaternion.identity : Quaternion.Normalize(rotation);
        }
    }
}
