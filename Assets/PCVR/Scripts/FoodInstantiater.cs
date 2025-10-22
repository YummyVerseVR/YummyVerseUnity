using System;
using Cysharp.Threading.Tasks;
using PCVR.Model.Interfaces;
using UnityEngine;
using Zenject;
using GLTFast;
using Oculus.Interaction;
using UnityEngine.SceneManagement;

namespace PCVR
{
    public class FoodInstantiater : MonoBehaviour
    {
        [Inject] private IQRCodeManager QRCodeManager;
        private string endpoint = "http://yummy-control-server.upiscium.f5.si:8000/";
        // private string endpoint = "http://localhost:8001/";
        private string id = "7b998836-903e-4878-ae8e-839a2ef13373";

        void Start()
        {
           InstantiateGLB().Forget();
        }

        private async UniTask InstantiateGLB()
        {
            // string url = endpoint + QRCodeManager.UserId + "/model/";
            string url = endpoint + QRCodeManager.UserId + "/model/";
            Debug.Log(url);
            var gltfImport = new GltfImport();
            var success = await gltfImport.Load(url);
            // 読み込みが成功した場合
            if (success)
            {
                // 読み込んだglTFのメインシーンを、このスクリプトがアタッチされているGameObjectの子として非同期でインスタンス化します。
                await gltfImport.InstantiateMainSceneAsync(this.transform);
                this.transform.localScale = Vector3.one * 0.3f;
                Debug.Log("Successfully loaded and instantiated GLB from URL.");
            }
            else
            {
                // 読み込みに失敗した場合
                Debug.LogError($"Failed to load GLB from URL: {url}");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene("Title");
            }
        }
    }
}
