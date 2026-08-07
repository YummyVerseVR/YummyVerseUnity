namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// アプリ全体の粗いモード。チュートリアルの内部進行はここに持ち込まない。
    /// </summary>
    public enum AppState
    {
        Attract,   // 待機・アトラクトループ。来場者を待つ
        Tutorial,  // チュートリアル進行中
        FreePlay,  // 自由体験(注文〜完食)
        Outro      // 締めの表示 → Attract へ戻る
    }
}
