using Food3DModel.Model;
using System;

namespace Food3DModel.Interface
{
    public interface IFoodDBHandler
    {
        bool Request(Guid userId); //リクエスト成功かどうか返す。
    }
}