namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// ゲーム側が発火するイベントの識別子。
    /// TutorialCondition のアセットからインスペクタで指定するために enum にしている。
    /// 増やすときは IGameEventBus / GameEventBus の配線も併せて追加すること。
    /// </summary>
    public enum GameEventId
    {
        StartButtonPressed,
        QrPlateDetected,
        QrPlateLost,
        FoodScooped,
        DishCleared,
        MenuItemSelected,
        UserAbsent
    }
}
