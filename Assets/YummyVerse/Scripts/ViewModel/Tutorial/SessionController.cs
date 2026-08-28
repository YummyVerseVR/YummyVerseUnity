using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;
using Zenject;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    /// <summary>
    /// Attract → Tutorial → FreePlay → Outro → Attract を回し続ける常駐ループ。
    ///
    /// 中断は _sessionCts の一括キャンセルだけで処理する。
    /// finally で必ず ResetToAttract を通るため、どこで離脱されても次の来場者に状態が残らない。
    /// </summary>
    public class SessionController : ISessionController, IInitializable, IDisposable
    {
        private readonly ITutorialRunner _runner;
        private readonly IFreePlayFlow _freePlay;
        private readonly TutorialContext _ctx;
        private readonly TutorialConfig _config;
        private readonly IAppStateMachine _appState;
        private readonly IGameEventBus _events;
        private readonly IGameEventPublisher _eventPublisher;
        private readonly IGameResetter _gameResetter;
        private readonly IIdleWatcher _idleWatcher;
        private readonly IInputLayer _inputLayer;
        private readonly IFoodPlacementService _foodPlacementService;

        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _lifetimeCts = new();

        private CancellationTokenSource _sessionCts;

        public SessionController(
            ITutorialRunner runner,
            IFreePlayFlow freePlay,
            TutorialContext ctx,
            TutorialConfig config,
            IAppStateMachine appState,
            IGameEventBus events,
            IGameEventPublisher eventPublisher,
            IGameResetter gameResetter,
            IIdleWatcher idleWatcher,
            IInputLayer inputLayer,
            IFoodPlacementService foodPlacementService)
        {
            _runner = runner;
            _freePlay = freePlay;
            _ctx = ctx;
            _config = config;
            _appState = appState;
            _events = events;
            _eventPublisher = eventPublisher;
            _gameResetter = gameResetter;
            _idleWatcher = idleWatcher;
            _inputLayer = inputLayer;
            _foodPlacementService = foodPlacementService;
        }

        public void Initialize()
        {
            _idleWatcher.IdleTimeoutSeconds = _config.IdleTimeoutSeconds;

            // 中断トリガー。ステップ側には一切の戻り線を書かない。
            Observable.FromEvent(
                    h => _events.OnUserAbsent += h,
                    h => _events.OnUserAbsent -= h)
                .Subscribe(_ => AbortSession("無操作/人検知ロスト")).AddTo(_disposables);

            Observable.FromEvent(
                    h => _inputLayer.OnStaffResetPressed += h,
                    h => _inputLayer.OnStaffResetPressed -= h)
                .Subscribe(_ => AbortSession("スタッフによるリセット")).AddTo(_disposables);

            Observable.FromEvent(
                    h => _ctx.AbortRequested += h,
                    h => _ctx.AbortRequested -= h)
                .Subscribe(_ => AbortSession("救済ポリシー ReturnToAttract")).AddTo(_disposables);

            RunLoopAsync(_lifetimeCts.Token).Forget();
        }

        public void AbortSession() => AbortSession("外部からの要求");

        private void AbortSession(string reason)
        {
            if (_sessionCts == null || _sessionCts.IsCancellationRequested) return;
            Debug.Log($"[Session] 中断します: {reason}");
            _sessionCts.Cancel();
        }

        private async UniTaskVoid RunLoopAsync(CancellationToken lifetimeCt)
        {
            while (!lifetimeCt.IsCancellationRequested)
            {
                try
                {
                    await WaitInAttractAsync(lifetimeCt);
                    await RunSessionAsync(lifetimeCt);
                }
                catch (OperationCanceledException)
                {
                    // アプリ終了。ループを抜ける。
                    return;
                }
                catch (Exception e)
                {
                    // ループ自体は止めない。展示中に1回の例外で無人稼働が死ぬのを防ぐ。
                    Debug.LogException(e);
                    await UniTask.Yield();
                }
            }
        }

        /// <summary>
        /// 仕様書 S1「ボタンを押してスタート」に相当する待機。
        /// AppState.Attract が「来場者を待つ」を担うため、ステップ列ではなくここで扱う。
        /// </summary>
        private async UniTask WaitInAttractAsync(CancellationToken lifetimeCt)
        {
            _appState.TrySet(AppState.Attract);
            _idleWatcher.SetActive(false);

            await WaitForFoodPlacementAsync(lifetimeCt);

            await _ctx.Message.ShowAsync(_config.AttractMessage, lifetimeCt);
            _ctx.Voice.PlayAsync(_config.AttractVoiceClip, lifetimeCt).SuppressCancellationThrow().Forget();

            await _events.GetStream(GameEventId.StartButtonPressed).FirstAsync(lifetimeCt);

            _ctx.Voice.Stop();
            await _ctx.Message.HideAsync(lifetimeCt);
        }

        /// <summary>
        /// 食べ物の表示位置が未設定のままセッションを始めると、食品モデルは読み込まれても
        /// 表示先の Transform が無いため何も見えない(チュートリアルの前菜が出ない不具合)。
        /// そのためスタート待ちの手前で、設定画面での位置指定を待つ。
        /// </summary>
        private async UniTask WaitForFoodPlacementAsync(CancellationToken lifetimeCt)
        {
            if (_foodPlacementService.IsPlacementConfigured.Value) return;

            Debug.Log("[Session] 食べ物の表示位置が未設定です。設定されるまでスタートを待ちます。");
            await _ctx.Message.ShowAsync(_config.FoodPlacementRequiredMessage, lifetimeCt);
            await _foodPlacementService.IsPlacementConfigured
                .Where(isConfigured => isConfigured)
                .FirstAsync(lifetimeCt);
            await _ctx.Message.HideAsync(lifetimeCt);
        }

        private async UniTask RunSessionAsync(CancellationToken lifetimeCt)
        {
            _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCt);
            var ct = _sessionCts.Token;

            try
            {
                _ctx.ResetForNewSession();
                _idleWatcher.SetActive(true);

                _appState.TrySet(AppState.Tutorial);
                await _runner.RunAsync(_config.MainSequence, _ctx, ct);

                // シーン遷移もロードも暗転もなく、そのまま実体験へ移行する
                _appState.TrySet(AppState.FreePlay);
                await _freePlay.RunAsync(_ctx, ct);

                _appState.TrySet(AppState.Outro);
            }
            catch (OperationCanceledException)
            {
                // 想定内。中断されただけ。
                Debug.Log("[Session] セッションが中断されました");
                lifetimeCt.ThrowIfCancellationRequested();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                await ResetToAttractAsync();

                _sessionCts.Dispose();
                _sessionCts = null;
            }
        }

        /// <summary>
        /// 中断のキャンセルに巻き込まれてリセットが中途半端に終わらないよう、
        /// ここでは意図的に CancellationToken.None を使う。
        /// </summary>
        private async UniTask ResetToAttractAsync()
        {
            var ct = CancellationToken.None;

            await _ctx.Message.HideAsync(ct);
            await _ctx.Hint.HideAsync(ct);
            await _ctx.Feedback.HideAsync(ct);
            await _ctx.Choice.HideAsync(ct);
            _ctx.Voice.Stop();

            await _gameResetter.ResetAsync(ct);

            _eventPublisher.ResetSessionState();
            _ctx.ResetForNewSession();
            _appState.TrySet(AppState.Attract);
        }

        public void Dispose()
        {
            _disposables?.Dispose();

            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            _sessionCts = null;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
        }
    }
}
