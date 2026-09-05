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
    /// 食品をどこに出すかを決め、物理空間に貼り付けたまま保つ。
    ///
    /// 置き場所は必ず <see cref="IPlacementReferenceFrame"/> 基準の相対姿勢で持つ。
    /// ワールド姿勢で持ってはいけない。HMD を被り直すとランタイムがワールド原点を
    /// 張り直すため、ワールド座標は「部屋のどこか」を表さなくなる。
    ///
    /// かつては Meta Spatial Anchor を基準にしていたが、このプロジェクトの構成
    /// (Unity OpenXR Plugin + Meta XR SDK) ではアンカーの保存が
    /// <c>XR_FB_spatial_entity_storage</c> 拡張なしで失敗し、PCVR では特に通らない。
    /// アンカーが作れないと「アンカー設定」も「位置固定」も完了できず、
    /// 設定画面のドラフト (ワールド姿勢) のまま運用されて着脱のたびにずれていた。
    /// そのため基準は部屋固定の参照フレームへ移した。
    /// </summary>
    public sealed class FoodPlacementService : IFoodPlacementService, IInitializable, ITickable, IDisposable
    {
        /// <summary>基準フレームが立ち上がるのを待つ上限 (秒)。</summary>
        private const float ReferenceFrameWaitSeconds = 10f;

        private readonly IPlacementReferenceFrame _referenceFrame;
        private readonly IFoodPlacementStore _placementStore;
        private readonly CancellationTokenSource _lifetimeCancellation = new();

        private Transform _foodPlacementRoot;

        /// <summary>設定画面の配置モデルの姿勢。基準が立っている間は基準フレーム基準で持つ。</summary>
        private Pose _draftPose;
        private bool _hasDraftPose;
        private bool _isDraftFrameRelative;

        /// <summary>いま食品を出している置き場所の姿勢。意味は <see cref="_draftPose"/> と同じ。</summary>
        private Pose _activePose;
        private bool _hasActivePose;
        private bool _isActiveFrameRelative;

        public ReactiveProperty<Transform> FoodTransform { get; } = new();
        public ReactiveProperty<FoodPlacementState> State { get; } = new(FoodPlacementState.Unconfigured);
        public ReactiveProperty<string> StatusMessage { get; } = new("Spatial placement is not configured.");
        public ReactiveProperty<bool> IsAnchorReady { get; } = new(false);
        public ReactiveProperty<bool> IsFoodPositionFixed { get; } = new(false);
        public ReactiveProperty<bool> IsConfigurationVisible { get; } = new(false);
        public ReactiveProperty<bool> IsBusy { get; } = new(false);
        public ReactiveProperty<bool> IsPlacementConfigured { get; } = new(false);

        public FoodPlacementService(
            IPlacementReferenceFrame referenceFrame,
            IFoodPlacementStore placementStore)
        {
            _referenceFrame = referenceFrame;
            _placementStore = placementStore;
        }

        public void Initialize()
        {
            RestoreAsync(_lifetimeCancellation.Token).Forget();
        }

        /// <summary>
        /// 置き場所を現在の基準フレームに繋ぎ直し続ける。
        /// 基準フレームは毎フレーム物理空間へ合わせ直されるので、その子で居続ける限り
        /// 食品は現実の同じ場所に留まる。繋ぎ直しを怠ると取り残される。
        /// </summary>
        public void Tick()
        {
            if (!_hasActivePose) return;

            var frame = _referenceFrame.Current;

            // 基準が後から立ち上がったら、ワールドで持っていた暫定値をそこへ移す。
            if (frame != null && !_isActiveFrameRelative)
            {
                ApplyPlacement();
                return;
            }

            if (_foodPlacementRoot == null)
            {
                ApplyPlacement();
                return;
            }

            if (ReferenceEquals(_foodPlacementRoot.parent, frame)) return;
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
                    : "Move or rotate the food model, then set the placement origin.";
            }
            else if (!isVisible && !IsBusy.Value && IsFoodPositionFixed.Value)
            {
                State.Value = FoodPlacementState.Ready;
                StatusMessage.Value = "Food position is fixed to the room.";
            }
        }

        /// <summary>
        /// 設定用モデルのワールド姿勢を受け取る。受け取った時点で基準フレーム基準へ直す。
        /// 「使うときに直す」では間に合わない。使うのは何分も後で、その間に
        /// 被り直しが挟まればワールド座標の意味が変わってしまう。
        /// </summary>
        public void UpdateDraftPose(Pose pose)
        {
            var frame = _referenceFrame.Current;
            _isDraftFrameRelative = frame != null;
            _draftPose = _isDraftFrameRelative ? ToFrameLocalPose(frame, pose) : pose;
            _hasDraftPose = true;
            RefreshPlacementConfigured();
        }

        /// <summary>
        /// 設定用モデルの最新の姿勢を、次に表示する食品の基準Transformへ反映する。
        /// 永続化前でもチュートリアル food をプレビューと同じ場所・回転に出せる。
        /// </summary>
        public bool TryActivateDraftPoseForFood()
        {
            if (_hasDraftPose)
            {
                _activePose = _draftPose;
                _isActiveFrameRelative = _isDraftFrameRelative;
                _hasActivePose = true;
            }

            if (!_hasActivePose) return false;

            ApplyPlacement();
            return FoodTransform.Value != null;
        }

        public bool TryGetSuggestedDraftPose(out Pose pose)
        {
            var frame = _referenceFrame.Current;

            if (_hasDraftPose && TryResolveWorldPose(_draftPose, _isDraftFrameRelative, frame, out pose))
            {
                return true;
            }

            if (_hasActivePose && TryResolveWorldPose(_activePose, _isActiveFrameRelative, frame, out pose))
            {
                return true;
            }

            pose = default;
            return false;
        }

        /// <summary>
        /// 「配置の基準を用意する」操作。かつては Meta Spatial Anchor を作って保存していたが、
        /// いまは部屋固定の基準フレームが立ち上がっているかを確認するだけでよい。
        /// ここで待つのは、起動直後にガーディアン由来の基準がまだ来ていないことがあるため。
        /// </summary>
        public async UniTask<bool> SetAnchorAtDraftAsync(CancellationToken cancellationToken)
        {
            if (!_hasDraftPose || IsBusy.Value) return false;

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token);

            IsBusy.Value = true;
            State.Value = FoodPlacementState.Saving;
            StatusMessage.Value = "Preparing the placement origin...";

            try
            {
                if (!await WaitForReferenceFrameAsync(linkedCancellation.Token))
                {
                    IsAnchorReady.Value = false;
                    State.Value = FoodPlacementState.Error;
                    StatusMessage.Value =
                        "Could not establish a room-fixed origin. Set up the headset boundary (Guardian) and retry.";
                    return false;
                }

                // 基準が立った。ワールドで持っていた下書きをその基準へ移す。
                RebaseDraftOnCurrentFrame();

                IsAnchorReady.Value = true;
                IsFoodPositionFixed.Value = false;
                State.Value = FoodPlacementState.Editing;
                StatusMessage.Value = "Origin ready. Move or rotate the food model, then lock its position.";
                return true;
            }
            catch (OperationCanceledException)
            {
                State.Value = FoodPlacementState.Error;
                StatusMessage.Value = "Placement setup was canceled. Open settings to retry.";
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                State.Value = FoodPlacementState.Error;
                StatusMessage.Value = $"Placement setup failed: {exception.Message}";
                return false;
            }
            finally
            {
                IsBusy.Value = false;
            }
        }

        public async UniTask<bool> FixFoodPositionAtDraftAsync(CancellationToken cancellationToken)
        {
            var frame = _referenceFrame.Current;
            if (!_hasDraftPose || frame == null || IsBusy.Value)
            {
                StatusMessage.Value = "Set the placement origin before locking the food position.";
                return false;
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token);

            IsBusy.Value = true;
            State.Value = FoodPlacementState.Saving;
            StatusMessage.Value = "Saving food position...";

            var localPose = _isDraftFrameRelative ? _draftPose : ToFrameLocalPose(frame, _draftPose);

            var newData = new FoodPlacementData
            {
                SchemaVersion = FoodPlacementData.CurrentSchemaVersion,
                ReferenceFrame = _referenceFrame.Kind,
                HasFoodPose = true,
                LocalPosition = localPose.position,
                LocalRotation = NormalizeRotation(localPose.rotation)
            };

            try
            {
                linkedCancellation.Token.ThrowIfCancellationRequested();
                _placementStore.Save(newData);

                _draftPose = localPose;
                _isDraftFrameRelative = true;
                _activePose = localPose;
                _isActiveFrameRelative = true;
                _hasActivePose = true;
                ApplyPlacement();

                IsAnchorReady.Value = true;
                IsFoodPositionFixed.Value = true;
                State.Value = FoodPlacementState.Ready;
                StatusMessage.Value = "Food position is fixed to the room.";
                Debug.Log(
                    $"[FoodPlacement] 食品位置を基準フレーム '{newData.ReferenceFrame}' に固定しました "
                    + $"(local pos {localPose.position} / world pos {FoodTransform.Value?.position}).");
                return true;
            }
            catch (OperationCanceledException)
            {
                RestoreStatusAfterFailedSave("Food position was not changed.");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                RestoreStatusAfterFailedSave($"Could not save the food position: {exception.Message}");
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
            if (!_placementStore.TryLoad(out var data))
            {
                State.Value = FoodPlacementState.Unconfigured;
                StatusMessage.Value = "Spatial placement is not configured.";
                return;
            }

            // 基準フレームの立ち上がりを待つ。IsBusy を見ている側はこの間
            // 「表示位置が無い」ではなく「決着していない」として扱うこと。
            IsBusy.Value = true;
            State.Value = FoodPlacementState.Loading;
            StatusMessage.Value = "Waiting for the room-fixed origin...";

            try
            {
                if (!await WaitForReferenceFrameAsync(cancellationToken))
                {
                    State.Value = FoodPlacementState.Error;
                    StatusMessage.Value =
                        "Could not establish a room-fixed origin. Set up the headset boundary (Guardian) and configure the placement again.";
                    Debug.LogWarning("[FoodPlacement] 部屋基準が立ち上がらず、保存済みの配置を復元できませんでした。");
                    return;
                }

                if (!data.MatchesFrame(_referenceFrame.Kind))
                {
                    State.Value = FoodPlacementState.Unconfigured;
                    StatusMessage.Value = "The saved placement uses a different origin. Configure it again.";
                    Debug.LogWarning(
                        $"[FoodPlacement] 保存済みの配置は基準 '{data.ReferenceFrame}' で測られており、"
                        + $"いまの基準 '{_referenceFrame.Kind}' では使えません。設定し直してください。");
                    return;
                }

                _activePose = new Pose(data.LocalPosition, NormalizeRotation(data.LocalRotation));
                _isActiveFrameRelative = true;
                _hasActivePose = true;
                ApplyPlacement();

                IsAnchorReady.Value = true;
                IsFoodPositionFixed.Value = true;
                Debug.Log(
                    $"[FoodPlacement] 保存済みの配置を復元しました (基準 '{data.ReferenceFrame}' / "
                    + $"local pos {data.LocalPosition} / world pos {FoodTransform.Value?.position}).");

                if (IsConfigurationVisible.Value)
                {
                    State.Value = FoodPlacementState.Editing;
                    StatusMessage.Value = "Move or rotate the food model, then lock its position.";
                }
                else
                {
                    State.Value = FoodPlacementState.Ready;
                    StatusMessage.Value = "Food position restored.";
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
        /// 基準フレームが使えるようになるまで待つ。起動直後やガーディアン取得前は
        /// 数秒かかることがあるので、一度で諦めない。
        /// </summary>
        private async UniTask<bool> WaitForReferenceFrameAsync(CancellationToken cancellationToken)
        {
            if (_referenceFrame.Current != null) return true;

            // PlayerLoop が回らない実行 (エディタのテストなど) では待てない。
            if (!Application.isPlaying) return false;

            var deadline = Time.unscaledTime + ReferenceFrameWaitSeconds;
            while (Time.unscaledTime < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                if (_referenceFrame.Current != null) return true;
            }

            return false;
        }

        /// <summary>
        /// 置き場所 Transform を現在の基準フレームの子として置き直す。
        /// ここでワールド姿勢を焼き付けてはいけない。
        /// </summary>
        private void ApplyPlacement()
        {
            if (!_hasActivePose) return;

            var frame = _referenceFrame.Current;

            if (_isActiveFrameRelative && frame == null)
            {
                // 基準がまだ (もう) 無い。ローカル姿勢をワールドとして置くと
                // 現実と無関係な場所に食品が出る。解決できるまで作らない。
                return;
            }

            if (frame != null && !_isActiveFrameRelative)
            {
                _activePose = ToFrameLocalPose(frame, _activePose);
                _isActiveFrameRelative = true;
            }

            if (_foodPlacementRoot == null)
            {
                _foodPlacementRoot = new GameObject("Food Placement Root").transform;
            }

            if (frame != null)
            {
                if (!ReferenceEquals(_foodPlacementRoot.parent, frame))
                {
                    _foodPlacementRoot.SetParent(frame, false);
                }
                _foodPlacementRoot.localPosition = _activePose.position;
                _foodPlacementRoot.localRotation = NormalizeRotation(_activePose.rotation);
            }
            else
            {
                // 基準が無い暫定表示。被り直しをまたぐと現実に対してずれる。
                // 設定を保存してもらうまでの繋ぎでしかない。
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

        private void RebaseDraftOnCurrentFrame()
        {
            var frame = _referenceFrame.Current;
            if (frame == null || _isDraftFrameRelative) return;

            _draftPose = ToFrameLocalPose(frame, _draftPose);
            _isDraftFrameRelative = true;
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

        private void RestoreStatusAfterFailedSave(string message)
        {
            var hasPreviousPlacement = FoodTransform.Value != null;
            IsAnchorReady.Value = _referenceFrame.Current != null;
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
        /// </summary>
        private void RefreshPlacementConfigured()
        {
            IsPlacementConfigured.Value = _hasDraftPose || FoodTransform.Value != null;
        }

        /// <summary>ワールド姿勢を基準フレーム基準へ直す。スケールに依存しない素の相対姿勢を使う。</summary>
        private static Pose ToFrameLocalPose(Transform frame, Pose worldPose)
        {
            var inverseFrameRotation = Quaternion.Inverse(frame.rotation);
            return new Pose(
                inverseFrameRotation * (worldPose.position - frame.position),
                NormalizeRotation(inverseFrameRotation * worldPose.rotation));
        }

        /// <summary>
        /// 保持している姿勢をワールドへ戻す。基準フレーム基準なのに基準が無いときは
        /// 意味のある値を作れないので false を返す。ローカル値をワールドとして使い回さない。
        /// </summary>
        private static bool TryResolveWorldPose(
            Pose pose,
            bool isFrameRelative,
            Transform frame,
            out Pose worldPose)
        {
            if (!isFrameRelative)
            {
                worldPose = pose;
                return true;
            }

            if (frame == null)
            {
                worldPose = default;
                return false;
            }

            worldPose = new Pose(
                frame.position + frame.rotation * pose.position,
                NormalizeRotation(frame.rotation * pose.rotation));
            return true;
        }

        private static Quaternion NormalizeRotation(Quaternion rotation)
        {
            return rotation == default ? Quaternion.identity : Quaternion.Normalize(rotation);
        }
    }
}
