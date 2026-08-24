using System;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Dummies
{
    /// <summary>
    /// サーバーに依存せずローカルからGltfを読み込む
    /// </summary>
    public class DummyFoodFetchable : IFoodFetchable
    {
        
        
        public async UniTask<FoodDownloadResult> Download(MenuItem item, CancellationToken ct)
        {
            var result = new FoodDownloadResult
            {
                RequestedGuid = item.Guid,
                RequestedItemId = item.Id
            };
            var gltfPath = Application.persistentDataPath + "/TestData/test.glb";

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
