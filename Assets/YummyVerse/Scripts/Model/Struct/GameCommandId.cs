namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// チュートリアル/FreePlay からゲーム側へ「やってほしいこと」を依頼するための識別子。
    /// 仕様書 S7 / S17 のような「ゲームに何かさせる」処理を、専用ステップ型を作らずに表現する。
    /// チュートリアルがゲームのコンポーネントを直接参照しないための一方向の口であり、
    /// 実際の実行は GameCommandRouter が既存の ViewModel に委譲する。
    /// </summary>
    public enum GameCommandId
    {
        None = 0,
        ServeRandomPersistentFood, // S8のすくう指示と同時(S7): PersistentDataPath/Foods からランダムに1品出す
        DestroyAllFood,   // 救済(ForceComplete) / セッションリセット
        ShowMenu,         // S16: メニューUIを出す
        HideMenu,
        ForceScoopFood,   // 救済: すくえないまま滞留したときに1回すくったことにする
        ForceClearDish    // 救済: 完食できないまま滞留したときに残りを食べ切ったことにする
    }
}
