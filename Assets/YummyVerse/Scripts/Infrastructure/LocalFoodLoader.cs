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

namespace YummyVerse.Scripts.Infrastructure
{
    public sealed class LocalFoodLoader : ILocalFoodModelLoader
    {
        private readonly LocalFoodSO _localFoodSO;

        public LocalFoodLoader(LocalFoodSO localFoodSO)
        {
            _localFoodSO = localFoodSO;
        }
        
        public async UniTask<FoodDownloadResult> LoadAsync(MenuItem item, CancellationToken ct)
        {
            var result = new FoodDownloadResult
            {
                RequestedGuid = item.Guid,
                RequestedItemId = item.Id
            };

            string gltfPath;
            if (item.Source == MenuItemSource.PersistentData)
            {
                gltfPath = item.ModelLocation;
            }
            else
            {
                if (item.Source != MenuItemSource.BuiltIn ||
                    !_localFoodSO.TryGetLocalFood(item.Guid, out var localFood))
                {
                    result.StatusCode = HttpStatusCode.NotFound;
                    return result;
                }

                var foodNameStr = localFood switch
                {
                    LocalFoods.Curry => "curry.glb",
                    LocalFoods.Shrimp => "shrimp.glb",
                    LocalFoods.Hamburg => "hamburg.glb",
                    LocalFoods.DragonSteak => "dragonsteak.glb",
                    _ => null,
                };

                if (string.IsNullOrWhiteSpace(foodNameStr))
                {
                    result.StatusCode = HttpStatusCode.NotFound;
                    return result;
                }

                gltfPath = Path.Combine(Application.persistentDataPath, "TestData", foodNameStr);
            }

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
                    // 咀嚼音はモデルと同じフォルダの audio ファイル。無ければ null のままで、
                    // 既定の咀嚼音が使われる。
                    var chewSound = await ChewSoundLoader.LoadFromFileAsync(item.AudioLocation, ct);
                    result.Food = new Food { GltfImport = gltfImport, ChewSound = chewSound };
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // 破損・未対応形式は item 単位の失敗として閉じ、他の local item 選択を継続可能にする。
                Debug.LogWarning($"Standalone food could not be loaded: {exception.GetType().Name}");
                result.StatusCode = HttpStatusCode.InternalServerError;
            }

            return result;
        }
    }
}
