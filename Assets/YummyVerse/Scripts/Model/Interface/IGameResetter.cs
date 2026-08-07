using System.Threading;
using Cysharp.Threading.Tasks;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// セッション終了時にゲーム側の状態(皿上の食品、注文内容、認識状態)を初期化する。
    /// ここに漏れがあると2人目の来場者で破綻する。
    /// 状態を持つ機能を足したら、必ずこの実装にも追記すること。
    /// </summary>
    public interface IGameResetter
    {
        UniTask ResetAsync(CancellationToken ct);
    }
}
