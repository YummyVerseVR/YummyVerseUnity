using System;
using Food3DModel.Interface;
using UnityEngine;
using Zenject;

namespace Food3DModel.Model
{
    // DBを叩いて食べ物の3Dモデルと咀嚼音のデータを取得するクラス
    public class TestFoodDBHandler: IFoodDBHandler, IInitializable
    {
        [Inject] private IFoodRepositoryWriter _foodRepositoryWriter;
        
        public void Initialize()
        {
            Debug.Log(Application.persistentDataPath);
        }
        
        public bool Request(Guid userId)
        {
            _foodRepositoryWriter.Set3DModel(LoadtestGLBDataBase64());
            _foodRepositoryWriter.SetChewingSound(LoadtestChewSoundDataBase64());
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