using System;
using R3;
using YummyVerse.Scripts.InputActions;
using YummyVerse.Scripts.Model.Interface;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    public class InputLayer : IInputLayer, IInitializable, IDisposable
    {
        public event Action OnConfigUIButtonClicked;
        public event Action OnFoodDestroyButtonClicked;
        public event Action OnStartButtonPressed;
        public event Action OnStaffResetPressed;

        private RestaurantInput restaurantInput = new();
        
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        public void Initialize()
        {
            restaurantInput.Enable();
            
            // メニューボタン
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.TurnOnMenu.performed += h,
                    h => restaurantInput.Eating.TurnOnMenu.performed -= h)
                .Subscribe(_ => OnConfigUIButtonClicked?.Invoke()).AddTo(_disposables);
            
            // 食べ物破壊ボタン
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.DestroyFood.performed += h,
                    h => restaurantInput.Eating.DestroyFood.performed -= h)
                .Subscribe(_ => OnFoodDestroyButtonClicked?.Invoke()).AddTo(_disposables);

            // 決定/スタートボタン
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.Start.performed += h,
                    h => restaurantInput.Eating.Start.performed -= h)
                .Subscribe(_ => OnStartButtonPressed?.Invoke()).AddTo(_disposables);

            // スタッフ用リセットボタン
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.StaffReset.performed += h,
                    h => restaurantInput.Eating.StaffReset.performed -= h)
                .Subscribe(_ => OnStaffResetPressed?.Invoke()).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            restaurantInput.Disable();
            restaurantInput?.Dispose();
        }
    }
}