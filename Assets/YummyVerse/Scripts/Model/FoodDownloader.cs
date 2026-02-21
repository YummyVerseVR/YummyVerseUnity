using System;
using System.IO;
using System.Net;
using System.Threading;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Cysharp.Threading.Tasks;

namespace YummyVerse.Scripts.Model
{
    public class FoodDownloader : IFoodFetchable
    {
        private readonly IEndPointManager _endPointManager;

        public FoodDownloader(IEndPointManager endPointManager)
        {
            _endPointManager = endPointManager;
        }
        
        public async UniTask<FoodDownloadResult> Download(Guid guid,CancellationToken ct)
        {
            FoodDownloadResult result = new FoodDownloadResult() { RequestedGuid = guid };
            using UnityWebRequest req = UnityWebRequest.Get(_endPointManager.baseEndPointUrl + guid.ToString() + "/model");
            req.timeout = 10; // 10秒でタイムアウトするように設定

            try
            {
                await req.SendWebRequest().WithCancellation(ct); // モデルをダウンロードする
                result.StatusCode = (HttpStatusCode)req.responseCode;
            }
            catch (UnityWebRequestException)
            {
                result.StatusCode = req.responseCode > 0 ? (HttpStatusCode)req.responseCode : 0;
            }
            catch (OperationCanceledException) when (req.result == UnityWebRequest.Result.ConnectionError)
            {
                result.StatusCode = 0;
            }
            
            // 0(失敗) or 400番台 or 500番台ならエラーがあるので、モデルの読み込みは行わずにreturn
            if (result.StatusCode is >= (HttpStatusCode)400 or 0)
            {
                return result;
            }
            
            // base64デコードして一時ファイルとして保存（Application.temporaryCachePathを使用）
            string b64 = Convert.ToBase64String(req.downloadHandler.data);
            byte[] glbBytes = Convert.FromBase64String(b64);
            string tempPath = Path.Combine(Application.temporaryCachePath, "test.glb");
            await File.WriteAllBytesAsync(tempPath, glbBytes, ct);

            // glTFastでロード
            var gltf = GltfImportFactory.Create();
            var loaded = await gltf.Load(tempPath, cancellationToken: ct);
            if (!loaded)
            {
                result.StatusCode = HttpStatusCode.InternalServerError;
                return result;
            }
            result.Food.GltfImport = gltf;
            
            return result;
        }
    }
}
