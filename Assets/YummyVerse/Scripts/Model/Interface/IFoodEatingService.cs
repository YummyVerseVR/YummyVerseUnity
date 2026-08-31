using R3;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// 1皿分の食事の進行を持つゲーム機能側の口 (FR19, FR21〜FR23)。
    ///
    /// 「何回すくったか」だけを扱い、当たり判定・縮小演出・音や振動は持たない。
    /// FoodScooped / DishCleared はここから発行されるので、チュートリアルも FreePlay も
    /// 同じイベントを購読するだけでよく、この interface を知る必要はない。
    /// </summary>
    public interface IFoodEatingService
    {
        /// <summary>完食までに必要なすくいの回数。</summary>
        int TotalPortions { get; }

        /// <summary>残量 (1 = 手つかず、0 = 完食)。表示スケールの倍率としてそのまま使える。</summary>
        ReadOnlyReactiveProperty<float> RemainingFraction { get; }

        /// <summary>いま食べられる食べ物が皿の上にあるか。当たり判定の有効/無効に対応する。</summary>
        ReadOnlyReactiveProperty<bool> IsInteractable { get; }

        /// <summary>食べ物が表示され、当たり判定の準備ができたときに呼ぶ。残量を満杯へ戻す。</summary>
        void BeginFood();

        /// <summary>
        /// セッションリセット・表示失敗で食べ物が無くなったときに呼ぶ。
        /// 完食ではないので DishCleared は発行しない。
        /// </summary>
        void AbandonFood();

        /// <summary>
        /// 有効なすくいを1回反映する。成立したら FoodScooped を、それで完食なら DishCleared を1度だけ発行する。
        /// 皿が空、または既に完食済みなら false を返して何も発行しない。
        /// </summary>
        bool TryScoop();

        /// <summary>
        /// 救済用。残りを一気に食べ切ったことにして、完食まで進める。
        /// 実際のすくいと同じイベント経路を通るため、チュートリアルに専用の分岐が要らない。
        /// </summary>
        bool ForceClear();
    }
}
