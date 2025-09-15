using System;
using System.Buffers.Text;
using System.IO;
using Cysharp.Threading.Tasks;
using Food3DModel.Interface;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace Food3DModel.Model
{
    // DBを叩いて食べ物の3Dモデルと咀嚼音のデータを取得するクラス
    public class TestFoodDBHandler: IFoodDBHandler, IInitializable
    {
        [Inject] private IFoodRepositoryWriter _foodRepositoryWriter;
        
        private const string APIEndpoint = "http://upiscium.f5.si:8001/"; // 本番環境用のAPIエンドポイント
        // private const string APIEndpoint = "http://192.168.8.170:8000/"; // 仮のAPIエンドポイント。俺のmacのローカルIP(MCC WifiでのIP)
        
        public async void Initialize()
        {
            WWWForm form = new WWWForm();
            form.AddField("user_id", "7b998836-903e-4878-ae8e-839a2ef13373");
            using (UnityWebRequest req = UnityWebRequest.Post(APIEndpoint + "create/user", form))
            {
                // FastAPI の Form(...) は application/x-www-form-urlencoded を期待してるから
                req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                await req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Response: " + req.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("Error: " + req.error + "\n" + req.downloadHandler.text);
                }
            }
        }
        
        
        
        
        public async UniTask<bool> Request(Guid userId)
        {
            // 3Dモデルを読み込む
            using(UnityWebRequest req = UnityWebRequest.Get(APIEndpoint + userId + "/model"))
            {
                await req.SendWebRequest();
                string b64 = Convert.ToBase64String(req.downloadHandler.data);
                _foodRepositoryWriter.Set3DModel(b64);
            }
            // 咀嚼音を読み込む
            using(UnityWebRequest req = UnityWebRequest.Get(APIEndpoint + userId + "/audio"))
            {
                await req.SendWebRequest();
                string b64 = Convert.ToBase64String(req.downloadHandler.data);
                _foodRepositoryWriter.SetChewingSound(b64);
            }
            
            // QRコードを読み取ったことをDBに通知
            using(UnityWebRequest req = UnityWebRequest.Get(APIEndpoint + "notify/" + userId))
            {
                await req.SendWebRequest();
            }

            return true;
        }
        
        private string LoadtestGLBDataBase64()
        {
            byte[] data = System.IO.File.ReadAllBytes(Application.persistentDataPath + "/TestData/test.glb");
            string base64Data = Convert.ToBase64String(data);
            return base64Data;
        }
        
        private string LoadtestChewSoundDataBase64()
        {
            byte[] data = System.IO.File.ReadAllBytes(Application.persistentDataPath + "/TestData/test.mp3");
            string base64Data = Convert.ToBase64String(data);
            return base64Data;
        }
    }
}