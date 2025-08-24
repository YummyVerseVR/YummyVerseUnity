using Food3DModel.Interface;
using GLTFast;
using R3;
using UnityEngine;
using Zenject;

namespace Food3DModel.ViewModel
{
    public class Food3DModelViewModel: IFood3DModelViewModel
    {
        [Inject] IFood3DModelAPIHandler _food3DModelAPIHandler;
        
        public ReactiveProperty<GameObject> Food3DModel { get; }
        
        public Food3DModelViewModel()
        {
            Food3DModel = new ReactiveProperty<GameObject>(null);
        }
        
        public async UniTask LoadFood3DModel(string foodName)
        {
            var apiBody = _food3DModelAPIHandler.Request(foodName);
            if (apiBody.GlbData != null)
            {
                var tempFilePath = System.IO.Path.Combine(Application.temporaryCachePath, "tempFood.glb");
                System.IO.File.WriteAllBytes(tempFilePath, apiBody.GlbData);
                var gltf = new GltfImport();
                await gltf.LoadGltfBinary(apiBody.GlbData);
            }
            else
            {
                Debug.LogError("Received null GLB data from API.");
            }
        }
    }
}