using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// Meta XR SDK の振動 API を隔離する境界。
    /// ハンドトラッキング中や非対応デバイスでは何も起きないが、それは失敗ではない。
    /// </summary>
    public sealed class OvrScoopHaptics : IScoopHaptics, IDisposable
    {
        private const float PulseFrequency = 0.6f;

        private readonly CancellationTokenSource _lifetime = new();

        public void PlayScoopPulse(ScoopHand hand, float amplitude, float durationSeconds)
        {
            if (durationSeconds <= 0f || amplitude <= 0f) return;

            var controller = hand switch
            {
                ScoopHand.Left => OVRInput.Controller.LTouch,
                ScoopHand.Right => OVRInput.Controller.RTouch,
                _ => OVRInput.Controller.Active
            };

            PulseAsync(controller, Mathf.Clamp01(amplitude), durationSeconds).Forget();
        }

        private async UniTaskVoid PulseAsync(OVRInput.Controller controller, float amplitude, float durationSeconds)
        {
            try
            {
                OVRInput.SetControllerVibration(PulseFrequency, amplitude, controller);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(durationSeconds),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                // アプリ終了時のキャンセル。振動を止めるところまでは finally で必ず行う。
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Eating] 振動の再生に失敗しました: {exception.Message}");
            }
            finally
            {
                try
                {
                    OVRInput.SetControllerVibration(0f, 0f, controller);
                }
                catch (Exception)
                {
                    // 停止も失敗する環境では、そのまま無視して食事の進行を優先する。
                }
            }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
