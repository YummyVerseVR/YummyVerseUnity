using NUnit.Framework;
using R3;
using UnityEngine;
using Zenject;

namespace Food3DModel.Model
{
    public class FoodInstanceHolder
    {
        [Inject] IFoodRepositoryReader _foodRepositoryReader;
        
        private GameObject _foodInstance;

    }
}