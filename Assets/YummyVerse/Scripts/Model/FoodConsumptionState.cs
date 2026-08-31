using System;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// 一つの食品 instance に属する、表示技術から独立した食事残量の状態。
    /// Collider や effect はこの結果を描画へ反映し、状態そのものには保持しない。
    /// </summary>
    public sealed class FoodConsumptionState
    {
        public int TotalPortions { get; }
        public int RemainingPortions { get; private set; }
        public bool IsCleared => RemainingPortions == 0;
        public float RemainingFraction => (float)RemainingPortions / TotalPortions;

        public FoodConsumptionState(int totalPortions)
        {
            if (totalPortions <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalPortions),
                    totalPortions,
                    "Total portions must be greater than zero.");
            }

            TotalPortions = totalPortions;
            RemainingPortions = totalPortions;
        }

        /// <summary>
        /// 有効な食事 action を一回だけ反映する。
        /// 完食後は false を返し、残量も DishCleared 相当の通知も再発生させない。
        /// </summary>
        public bool TryConsume(out FoodConsumptionResult result)
        {
            if (IsCleared)
            {
                result = FoodConsumptionResult.NotConsumed(RemainingPortions, RemainingFraction);
                return false;
            }

            RemainingPortions--;
            result = FoodConsumptionResult.Consumed(
                RemainingPortions,
                RemainingFraction,
                IsCleared);
            return true;
        }
    }

    public readonly struct FoodConsumptionResult
    {
        public bool WasConsumed { get; }
        public bool DishCleared { get; }
        public int RemainingPortions { get; }
        public float RemainingFraction { get; }

        private FoodConsumptionResult(
            bool wasConsumed,
            bool dishCleared,
            int remainingPortions,
            float remainingFraction)
        {
            WasConsumed = wasConsumed;
            DishCleared = dishCleared;
            RemainingPortions = remainingPortions;
            RemainingFraction = remainingFraction;
        }

        internal static FoodConsumptionResult Consumed(
            int remainingPortions,
            float remainingFraction,
            bool dishCleared)
        {
            return new FoodConsumptionResult(
                true,
                dishCleared,
                remainingPortions,
                remainingFraction);
        }

        internal static FoodConsumptionResult NotConsumed(
            int remainingPortions,
            float remainingFraction)
        {
            return new FoodConsumptionResult(
                false,
                false,
                remainingPortions,
                remainingFraction);
        }
    }
}
