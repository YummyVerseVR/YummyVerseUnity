using Food3DModel.Model;
using System;

namespace Food3DModel.Interface
{
    public interface IFoodDBHandler
    {
        FoodDBBody Request(Guid userId);
    }
}