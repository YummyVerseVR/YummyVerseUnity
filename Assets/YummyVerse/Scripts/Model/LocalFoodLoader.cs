using System;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class LocalFoodLoader : IFoodFetchable
    {
        public async UniTask<FoodDownloadResult> Download(Guid guid, CancellationToken ct)
        {
            var result = new FoodDownloadResult() { RequestedGuid =  guid };
            var gltfPath = Application.persistentDataPath + "/TestData/curry.glb";
            Debug.Log(gltfPath);

            if (string.IsNullOrWhiteSpace(gltfPath) || !File.Exists(gltfPath))
            {
                result.StatusCode = HttpStatusCode.NotFound;
                return result;
            }

            try
            {
                var gltfImport = new GltfImport();
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