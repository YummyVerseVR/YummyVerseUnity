using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using YummyVerse.Scripts.ViewModel.Tutorial.SO;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    /// <summary>
    /// 咀嚼計のキャリブレーション案内。
    ///
    /// 流れ:
    ///   1. 「口を動かさないでください」を出し、CAL_START を送る。
    ///   2. CAL_ACCEPTED から一定時間 (既定 5 秒) 後に「もぐもぐしてください」へ切り替える。
    ///   3. CAL_DONE で案内を閉じ、呼び出し元がチュートリアル本体 (S2) へ進む。
    ///
    /// 咀嚼計が繋がっていない・失敗した・応答が返らない場合でも例外にはせず、
    /// 案内を閉じて先へ進める。無人運用の展示で1台の不調が来場者を足止めしないようにするため。
    /// </summary>
    public sealed class ChewingCalibrationFlow : IChewingCalibrationFlow
    {
        private readonly IChewingSensorService _sensor;
        private readonly ChewingSensorConfig _sensorConfig;
        private readonly TutorialConfig _tutorialConfig;

        public ChewingCalibrationFlow(
            IChewingSensorService sensor, ChewingSensorConfig sensorConfig, TutorialConfig tutorialConfig)
        {
            _sensor = sensor;
            _sensorConfig = sensorConfig;
            _tutorialConfig = tutorialConfig;
        }

        public async UniTask RunAsync(TutorialContext ctx, CancellationToken ct)
        {
            if (!await WaitForSensorAsync(ct))
            {
                Debug.LogWarning("[ChewingSensor] 咀嚼計へ接続できないため、キャリブレーションを飛ばします");
                return;
            }

            await ctx.Message.ShowAsync(_tutorialConfig.ChewingCalibrationHoldMessage, ct);

            // 「もぐもぐしてください」への切り替えは CAL_ACCEPTED を起点に走らせ、
            // 較正が決着した時点で打ち切る。CAL_DONE の方が先に来たら案内を出さずに終える。
            using var promptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var acceptedSource = new UniTaskCompletionSource();
            ShowChewPromptAsync(ctx, acceptedSource, promptCts.Token).Forget();

            ChewingCalibrationResult result;
            try
            {
                result = await _sensor.CalibrateAsync(() => acceptedSource.TrySetResult(), ct);
            }
            finally
            {
                promptCts.Cancel();
            }

            if (!result.IsSuccess)
            {
                Debug.LogWarning($"[ChewingSensor] キャリブレーションが完了しませんでした ({result})。そのまま続行します。");
            }

            await ctx.Message.HideAsync(ct);
        }

        /// <summary>
        /// COMポート探索は起動直後から常駐で走っているので、通常はここで待たずに通る。
        /// 咀嚼計を後から挿した場合に備えて、上限付きで接続を待つ。
        /// </summary>
        private async UniTask<bool> WaitForSensorAsync(CancellationToken ct)
        {
            if (_sensor.ConnectionState.CurrentValue == ChewingSensorConnectionState.Connected) return true;
            if (_sensorConfig.ConnectionWaitSeconds <= 0f) return false;

            Debug.Log("[ChewingSensor] 咀嚼計との接続を待っています");

            var deadline = Time.realtimeSinceStartup + _sensorConfig.ConnectionWaitSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (_sensor.ConnectionState.CurrentValue == ChewingSensorConnectionState.Connected) return true;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            return false;
        }

        private async UniTaskVoid ShowChewPromptAsync(
            TutorialContext ctx, UniTaskCompletionSource accepted, CancellationToken ct)
        {
            try
            {
                await accepted.Task.AttachExternalCancellation(ct);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_tutorialConfig.ChewingCalibrationChewPromptDelaySeconds),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: ct);

                await ctx.Message.ShowAsync(_tutorialConfig.ChewingCalibrationChewMessage, ct);
            }
            catch (OperationCanceledException)
            {
                // 較正が先に決着した。案内の切り替えは不要。
            }
        }
    }
}
