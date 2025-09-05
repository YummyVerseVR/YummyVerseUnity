using System;
using Food3DModel.Interface;
using R3;
using UnityEngine;
using Zenject;

namespace FoodDB.Scripts.ViewModel
{
    public class QRViewModel: IQRViewModel, IInitializable
    {
        [Inject] private IFoodDBHandler _dbHandler;
        [Inject] private IFoodRepositoryWriter _foodRepositoryWriter;
        
        private ReactiveProperty<string> _qrValue = new ReactiveProperty<string>();

        public void Initialize()
        {
            _qrValue.Subscribe(async v => 
            {
                if (Guid.TryParse(v, out Guid guid))
                {
                    var res = await _dbHandler.Request(guid);
                    if (!res)
                    {
                        Debug.LogErrorFormat($"このGUID \"{guid}\" は登録されていません");
                    }
                    else
                    {
                        Debug.Log("QRコード認識成功。　Guid: " + guid);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("不正なQRコードです。");
                }
            });
        }
        
        public void OnDetectQRCode(string value, Transform qrTransform)
        {
            if (_qrValue.Value != value)
            {
                _qrValue.Value = value;
                
            }
        }
    }
}