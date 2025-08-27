using NUnit.Framework;
using R3;
using UnityEngine;
using Zenject;

namespace Food3DModel.Model
{
    public class FoodInstanceHolder: IInitializable
    {
        [Inject] IFoodRepositoryReader _foodRepositoryReader;
        
        private GameObject _foodInstance;

        public void Initialize()
        {
            _foodRepositoryReader.Food3DModel.Where(v => v != null).Subscribe(v =>
            {
                Debug.Log("FoodInstanceHolder: 3Dモデルの更新");
                GameObject.Destroy(_foodInstance);
                _foodInstance = GameObject.Instantiate(v);
            });
        }
    }
}