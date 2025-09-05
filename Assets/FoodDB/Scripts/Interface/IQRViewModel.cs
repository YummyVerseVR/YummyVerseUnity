using R3;
using UnityEngine;

namespace Food3DModel.Interface
{
    public interface IQRViewModel
    {
        void OnDetectQRCode(string value, Transform qrTransform);
    }
}