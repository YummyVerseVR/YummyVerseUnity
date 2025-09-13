using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Food3DModel.Interface
{
    public interface IFoodRepositoryWriter
    {
        void Set3DModel(string glbBase64);
        UniTaskVoid SetChewingSound(string chewingSoundBase64);
        
        void SetFoodTransform(Transform transform);
    }
}