using System;
using Food3DModel.Interface;
using UnityEngine;

namespace Food3DModel.Model
{
    // DBを叩いて食べ物の3Dモデルと咀嚼音のデータを取得するクラス
    public class TestFoodDBHandler: IFoodDBHandler
    {
        public FoodDBBody Request(Guid userId)
        {
            return new FoodDBBody(
                LoadtestGLBDataBase64(),
                LoadtestChewSoundDataBase64()
            );
        }
        
        private string LoadtestGLBDataBase64()
        {
            byte[] data = System.IO.File.ReadAllBytes(Application.persistentDataPath + "/glbData.glb");
            string base64Data = Convert.ToBase64String(data);
            return base64Data;
        }
        
        private string LoadtestChewSoundDataBase64()
        {
            byte[] data = System.IO.File.ReadAllBytes(Application.persistentDataPath + "/chewSoundData.wav");
            string base64Data = Convert.ToBase64String(data);
            return base64Data;
        }
    }
}