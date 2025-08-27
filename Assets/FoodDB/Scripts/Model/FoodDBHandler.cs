using System;
using Cysharp.Threading.Tasks;
using Food3DModel.Interface;

namespace Food3DModel.Model
{
    public class FoodDBHandler: IFoodDBHandler
    {
        public UniTask<bool> Request(Guid userId)
        {
            throw new NotImplementedException();
        }
    }
}