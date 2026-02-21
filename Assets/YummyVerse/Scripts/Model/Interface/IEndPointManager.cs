namespace YummyVerse.Scripts.Model.Interface
{
    public interface IEndPointManager
    {
        string baseEndPointUrl { get; }
        /// <summary>
        /// APIのエンドポイントの更新を試みるメソッド
        /// URLのバリデーションを行い、その成否を返す。
        /// バリデーションでエラーとなった場合はエンドポイントのURLは更新されない。
        /// </summary>
        /// <param name="url">新たに登録するエンドポイントURL</param>
        /// <returns>URLのバリデーションの成否</returns>
        bool UpdateEndPointUrl(string url);
    }
}