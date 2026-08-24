using Food3DModel.Interface;
using UnityEngine;
using Zenject;

namespace FoodDB.Scripts.ViewModel
{
    public class QRViewModel: IQRViewModel
    {
        [Inject] private IFoodRepositoryWriter _foodRepositoryWriter;
        private string _lastQrValue;
        
        public void OnDetectQRCode(string value, Transform qrTransform)
        {
            if (_lastQrValue != value)
            {
                // QR は designation の入力として位置だけを更新する。
                // payload を食品 identity や旧 GUID download の起点にはしない。
                _lastQrValue = value;
                var newTransform = qrTransform;
                newTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                _foodRepositoryWriter.SetFoodTransform(newTransform);
            }
        }
    }
}
