using System;
using Cysharp.Threading.Tasks;
using Food3DModel.Interface;
using UnityEngine;

namespace Food3DModel.Model
{
    public class FoodDBHandler: IFoodDBHandler
    {
        public UniTask<bool> Request(Guid userId)
        {
            Debug.LogWarning(
                "YummyService v2 transport is unavailable because its path/auth/download contract has not been published.");
            return UniTask.FromResult(false);
        }
    }
}
