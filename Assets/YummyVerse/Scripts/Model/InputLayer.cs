using System;
using R3;
using UnityEngine.InputSystem;
using YummyVerse.Scripts.InputActions;
using YummyVerse.Scripts.Model.Interface;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    public class InputLayer : IInputLayer, IInitializable, IDisposable
    {
        public event Action OnConfigUIButtonClicked;
        public event Action OnStartButtonPressed;
        public event Action OnStaffResetPressed;

        private const string AButtonPath = "<XRController>{RightHand}/primaryButton";

        private readonly RestaurantInput restaurantInput = new();
        private bool _suppressAButtonRelease;
        
        private readonly CompositeDisposable _disposables = new();
        
        public void Initialize()
        {
            restaurantInput.Enable();
            
            // 設定メニューは A + X の同時押し。Input Action 側の Unordered composite により、
            // どちらを先に押しても、両方が押されている時間が重なれば成立する。
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.TurnOnMenu.performed += h,
                    h => restaurantInput.Eating.TurnOnMenu.performed -= h)
                .Subscribe(_ =>
                {
                    // A は単押し時に進行にも使う。A + X が成立したあとの A の release を
                    // 進行として二重処理しないよう、次の A release だけを抑止する。
                    _suppressAButtonRelease = true;
                    OnConfigUIButtonClicked?.Invoke();
                }).AddTo(_disposables);

            // A 単押しは release 時に進行させる。押下中に X が加わった場合は上の
            // A + X composite が先に成立し、release を設定メニュー操作として消費できる。
            Observable.FromEvent<InputAction.CallbackContext>(
                    h => restaurantInput.Eating.Start.started += h,
                    h => restaurantInput.Eating.Start.started -= h)
                .Where(IsAButton)
                .Subscribe(_ =>
                {
                    if (!restaurantInput.Eating.TurnOnMenu.IsPressed())
                    {
                        _suppressAButtonRelease = false;
                    }
                }).AddTo(_disposables);

            // ゲーム進行は A または B。開発用の Space binding も維持する。
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.Start.performed += h,
                    h => restaurantInput.Eating.Start.performed -= h)
                .Subscribe(ctx =>
                {
                    if (IsAButton(ctx) && _suppressAButtonRelease)
                    {
                        _suppressAButtonRelease = false;
                        return;
                    }

                    OnStartButtonPressed?.Invoke();
                }).AddTo(_disposables);

            // スタッフ用リセットボタン
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.StaffReset.performed += h,
                    h => restaurantInput.Eating.StaffReset.performed -= h)
                .Subscribe(_ => OnStaffResetPressed?.Invoke()).AddTo(_disposables);
        }

        private static bool IsAButton(InputAction.CallbackContext context)
        {
            return context.control != null && InputControlPath.Matches(AButtonPath, context.control);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            restaurantInput.Disable();
#if UNITY_EDITOR
            // InputAction の自動生成 Dispose は Object.Destroy を使うため、EditMode テストでは
            // Unity がエラーを出す。実行中は通常破棄、EditMode では即時破棄を使い分ける。
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(restaurantInput.asset);
                return;
            }
#endif
            restaurantInput.Dispose();
        }
    }
}
