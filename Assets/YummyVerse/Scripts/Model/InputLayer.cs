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

        private RestaurantInput restaurantInput = new();
        
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        public void Initialize()
        {
            restaurantInput.Enable();
            Observable.FromEvent<UnityEngine.InputSystem.InputAction.CallbackContext>(
                    h => restaurantInput.Eating.TurnOnMenu.performed += h,
                    h => restaurantInput.Eating.TurnOnMenu.performed -= h)
                .Subscribe(_ => OnConfigUIButtonClicked?.Invoke()).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            restaurantInput.Disable();
            restaurantInput?.Dispose();
        }
    }
}