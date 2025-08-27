using Food3DModel.Model;
using System;
using Cysharp.Threading.Tasks;

namespace Food3DModel.Interface
{
    public interface IFoodDBHandler
    {
        UniTask<bool> Request(Guid userId); //リクエスト成功かどうか返す。
    }
}