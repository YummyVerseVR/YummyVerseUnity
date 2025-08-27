using R3;
using UnityEngine;
using Zenject;

namespace Food3DModel.Model
{
    public class FoodInstanceHandler: IInitializable
    {
        [Inject] IFoodRepositoryReader _foodRepositoryReader;
        
        private GameObject _foodInstance;

        public void Initialize()
        {
            _foodRepositoryReader.Food3DModel.Where(v => v != null).Subscribe(v =>
            {
                GameObject.Destroy(_foodInstance);
                _foodInstance = GameObject.Instantiate(v);
            });
        }
    }
}