using System;
using Cysharp.Threading.Tasks;
using Food3DModel.Interface;
using GLTFast;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;
using R3;

namespace Food3DModel.View
{
    public class Food3DModelView: MonoBehaviour
    {
        [Inject] private IFood3DModelViewModel _viewModel;
        
        private GameObject _foodGameObject;

        public void Start()
        {
            _viewModel.FoodGLTF.Subscribe( v =>
            {
                if (v != null)
                {
                    UpdateModel(v).Forget();
                }
            }).AddTo(this);
        }
        
        private async UniTask UpdateModel(GltfImport gltf)
        {
            Assert.IsNotNull(gltf);
            await gltf.InstantiateMainSceneAsync(transform); // Todo: Instanceを保持するようにする
        }
    }
}