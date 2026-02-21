using System;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Scripts.Model
{
    public class LocalFoodLoader : IFoodFetchable
    {
        private readonly LocalFoodSO _localFoodSO;

        public LocalFoodLoader(LocalFoodSO localFoodSO)
        {
            _localFoodSO = localFoodSO;
        }
        
        public async UniTask<FoodDownloadResult> Download(Guid guid, CancellationToken ct)
        {
            var result = new FoodDownloadResult() { RequestedGuid =  guid };
            
            _localFoodSO.TryGetLocalFood(guid, out var  localFood);

            var foodNameStr = localFood switch
            {
                LocalFoods.Curry => "curry.glb",
                LocalFoods.Shrimp => "shrimp.glb",
                LocalFoods.Hamburg => "hamburg.glb"
            };

            var gltfPath = Application.persistentDataPath + "/TestData/" + foodNameStr;

            if (string.IsNullOrWhiteSpace(gltfPath) || !File.Exists(gltfPath))
            {
                result.StatusCode = HttpStatusCode.NotFound;
                return result;
            }

            try
            {
                var gltfImport = GltfImportFactory.Create();
                var loaded = await gltfImport.Load(gltfPath, cancellationToken: ct);
                result.StatusCode = loaded ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                if (loaded)
                {
                    result.Food = new Food { GltfImport = gltfImport };
                }
            }
            catch (IOException)
            {
                result.StatusCode = HttpStatusCode.InternalServerError;
            }
            catch (UnauthorizedAccessException)
            {
                result.StatusCode = HttpStatusCode.InternalServerError;
            }

            return result;
        }
    }
}
