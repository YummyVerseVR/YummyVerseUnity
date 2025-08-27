using System;

namespace Food3DModel.Model
{
    public class FoodDBBody
    {
        string glbData; // base64エンコードされたGLBデータ
        string chewSoundData; // base64エンコードされた咀嚼音データ
        
        public FoodDBBody(string glbData, string chewSoundData)
        {
            this.glbData = glbData;
            this.chewSoundData = chewSoundData;
        }
    }
}