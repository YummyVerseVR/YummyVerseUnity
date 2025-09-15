using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Food3DModel.Interface;
using GLTFast;
using R3;
using UnityEngine;

namespace Food3DModel.Model
{
    public class FoodRepository: IFoodRepositoryReader, IFoodRepositoryWriter
    {
        public ReactiveProperty<GameObject> Food3DModel { get; private set; }
        public ReactiveProperty<AudioClip> ChewingSound { get; private set; }
        
        public ReactiveProperty<Transform> FoodTransform { get; private set; }
        
        public FoodRepository()
        {
            Food3DModel = new ReactiveProperty<GameObject>();
            ChewingSound = new ReactiveProperty<AudioClip>();
            FoodTransform = new ReactiveProperty<Transform>();
        }
        
        public void SetFoodTransform(Transform transform)
        {
            FoodTransform.Value = transform;
        }
        
        public async void Set3DModel(string glbBase64)
        {
            try
            {
                // 1. base64エンコードされたglbをデコード
                byte[] glbBytes = Convert.FromBase64String(glbBase64);

                // 2. 一時ファイルとして保存（Application.temporaryCachePathを使用）
                string tempPath = Path.Combine(Application.temporaryCachePath, "test.glb");
                File.WriteAllBytes(tempPath, glbBytes);

                Debug.Log($"[ModelLoader] Saved temporary glb to: {tempPath}");

                // 3. glTFastでロード
                var gltf = new GltfImport();
                var success = await gltf.Load(tempPath);

                if (!success)
                {
                    Debug.LogError("[ModelLoader] Failed to load glb model.");
                    return;
                }

                // 4. 古いモデルがあれば破棄
                if (Food3DModel != null)
                {
                    GameObject.Destroy(Food3DModel.Value);
                }

                // 5. GameObjectにインスタンス化
                Food3DModel.Value = new GameObject("LoadedGLBModel");
                success = await gltf.InstantiateMainSceneAsync(FoodTransform.Value);

                if (!success)
                {
                    Debug.LogError("[ModelLoader] Failed to instantiate model.");
                }
                else
                {
                    FoodTransform.Value.Find("world").Rotate(90, 0,0);
                    Debug.Log("[ModelLoader] Successfully loaded and instantiated model.");
                    // Food3DModel.Value.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // スケール調整
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelLoader] Exception: {e.Message}\n{e.StackTrace}");
            }
        }
        

        public async UniTaskVoid SetChewingSound(string chewingSoundBase64)
        {
            AudioClip audioClip;
            
            // 1. base64エンコードされた音声データをデコード
            byte[] audioBytes = Convert.FromBase64String(chewingSoundBase64);
            // 2. 一時ファイルとして保存（Application.temporaryCachePathを使用）
            string tempPath = Path.Combine(Application.temporaryCachePath, "tempChewingSound.mp3");
            File.WriteAllBytes(tempPath, audioBytes);
            
            using (WWW www = new WWW("file://" + tempPath))  //※あくまでローカルファイルとする
            {
                await UniTask.WaitWhile(() => !www.isDone);

                audioClip = www.GetAudioClip(false, true);
                if (audioClip.loadState != AudioDataLoadState.Loaded)
                {
                    //ここにロード失敗処理
                    Debug.Log("Failed to load AudioClip.");
                }
            }
            ChewingSound.Value = audioClip;
        }
    }
}