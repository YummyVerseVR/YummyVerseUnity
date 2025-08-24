using Food3DModel.Interface;
using Food3DModel.Model;
using UnityEngine;

namespace Food3DModel
{
    public class DummyFood3DModelAPIHandler: IFood3DModelAPIHandler
    {
        public Food3DModelAPIBody Request(string foodName)
        {
            byte[] glbData = System.IO.File.ReadAllBytes(Application.persistentDataPath + "/food.glb");
            return new Food3DModelAPIBody(glbData);
        }
    }
}