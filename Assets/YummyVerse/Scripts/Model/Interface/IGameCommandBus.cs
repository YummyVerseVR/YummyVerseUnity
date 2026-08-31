using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// チュートリアル/FreePlay からゲーム側への依頼を流す一方向の口。
    /// 仕様書 §1.3 の「TutorialRunner から Game のロジックを直接呼び出してはならない」を守るため、
    /// 依頼は必ずここを経由し、実行はシーンスコープの GameCommandHandler に委譲する。
    /// </summary>
    public interface IGameCommandBus
    {
        Observable<GameCommandId> OnCommand { get; }

        void Request(GameCommandId command);
    }
}
