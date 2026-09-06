using System;
using System.Globalization;
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
    /// 流れ (プロトコル仕様書 §9.2):
    ///   1. 「口を閉じて動かさないでください」+ カウントダウン。0 で「計測中...」に変わり、
    ///      同時に CAL_NOISE が送られてノイズ測定が始まる。
    ///   2. ノイズ測定が終わったら「10回嚙んでください」+ カウントダウン。
    ///      0 で「計測中...」に変わり、同時に CAL_CHEW が送られる。
    ///   3. 閾値が確定したら案内を閉じ、呼び出し元がチュートリアル本体 (S2) へ進む。
    ///
    /// フェーズの順序と送信の判断は ChewingSensorService が持ち、ここは
    /// 「利用者の準備ができるまで待たせる」表示だけを担当する。
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

            var prompt = new CountdownPrompt(ctx, _tutorialConfig);
            var result = await _sensor.CalibrateAsync(prompt, ct);

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

        /// <summary>
        /// フェーズ開始前の案内とカウントダウン。
        ///
        /// カウントが 0 になった時点でこの待ちが明けて、呼び出し側がフェーズ要求を送る。
        /// 先に送ると、利用者が動作を始める前の値を測ってしまう (仕様書 §9.2)。
        /// </summary>
        private sealed class CountdownPrompt : IChewingCalibrationPrompt
        {
            private readonly TutorialContext _ctx;
            private readonly TutorialConfig _config;

            public CountdownPrompt(TutorialContext ctx, TutorialConfig config)
            {
                _ctx = ctx;
                _config = config;
            }

            public async UniTask PrepareAsync(ChewingCalibrationPhase phase, CancellationToken ct)
            {
                var instruction = phase == ChewingCalibrationPhase.Noise
                    ? _config.ChewingCalibrationNoiseMessage
                    : _config.ChewingCalibrationChewMessage;

                // 測定中の表示はカウント 0 の瞬間に差し替えるため、先に解決しておく。
                var measuring = await _config.ChewingCalibrationMeasuringMessage.ResolveAsync(ct);
                var seconds = Mathf.Max(0, _config.ChewingCalibrationCountdownSeconds);

                await _ctx.Message.ShowAsync(instruction, seconds > 0 ? Format(seconds) : measuring, ct);

                for (var remaining = seconds; remaining > 0; remaining--)
                {
                    _ctx.Message.SetSubText(Format(remaining));
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(1), DelayType.UnscaledDeltaTime, cancellationToken: ct);
                }

                // カウント 0。この直後に呼び出し側がフェーズ要求を送り、測定が始まる。
                _ctx.Message.SetSubText(measuring);
            }

            private static string Format(int seconds) => seconds.ToString(CultureInfo.InvariantCulture);
        }
    }
}
