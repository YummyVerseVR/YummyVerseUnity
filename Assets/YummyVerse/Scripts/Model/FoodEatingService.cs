using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// FoodConsumptionState (純粋な残量) と GameEventBus (共通イベント) を繋ぐゲーム機能。
    /// 当たり判定や見た目は持たず、View がここの RemainingFraction を見て縮小する。
    /// </summary>
    public sealed class FoodEatingService : IFoodEatingService, IDisposable
    {
        /// <summary>
        /// 完食までのすくい回数。要件に回数の規定が無いため既定値を 5 とする。
        /// </summary>
        public const int DefaultPortionsPerFood = 5;

        private readonly IGameEventPublisher _eventPublisher;
        private readonly ReactiveProperty<float> _remainingFraction = new(1f);
        private readonly ReactiveProperty<bool> _isInteractable = new(false);

        private FoodConsumptionState _state;

        public int TotalPortions { get; }
        public ReadOnlyReactiveProperty<float> RemainingFraction => _remainingFraction;
        public ReadOnlyReactiveProperty<bool> IsInteractable => _isInteractable;

        public FoodEatingService(IGameEventPublisher eventPublisher)
            : this(eventPublisher, DefaultPortionsPerFood)
        {
        }

        public FoodEatingService(IGameEventPublisher eventPublisher, int totalPortions)
        {
            if (totalPortions <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalPortions), totalPortions, "Total portions must be greater than zero.");
            }

            _eventPublisher = eventPublisher;
            TotalPortions = totalPortions;
        }

        public void BeginFood()
        {
            _state = new FoodConsumptionState(TotalPortions);
            _remainingFraction.Value = 1f;
            _isInteractable.Value = true;
            Debug.Log($"[Eating] 食べ物を提供しました。{TotalPortions} 回で完食します。");
        }

        public void AbandonFood()
        {
            _state = null;
            _isInteractable.Value = false;
            _remainingFraction.Value = 1f;
        }

        public bool TryScoop()
        {
            if (_state == null || !_isInteractable.CurrentValue) return false;
            if (!_state.TryConsume(out var result)) return false;

            _remainingFraction.Value = result.RemainingFraction;
            _eventPublisher.PublishFoodScooped();

            if (!result.DishCleared) return true;

            // 完食した瞬間に当たり判定を閉じる。以降のフレームの接触では二重発行しない。
            _isInteractable.Value = false;
            _eventPublisher.PublishDishCleared();
            return true;
        }

        public bool ForceClear()
        {
            if (_state == null || !_isInteractable.CurrentValue) return false;

            // 実際のすくいと同じ経路を通すため、残り回数ぶんの TryScoop を最後まで回す。
            var consumedAny = false;
            while (TryScoop()) consumedAny = true;
            return consumedAny;
        }

        public void Dispose()
        {
            _remainingFraction.Dispose();
            _isInteractable.Dispose();
        }
    }
}
