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

        private ReactiveProperty<string> _qrValue = new ReactiveProperty<string>();

        public void Initialize()
        {
            _qrValue.Subscribe(v =>
            {
                if (Guid.TryParse(v, out Guid guid))
                {
                    var res =_dbHandler.Request(guid);
                    if (!res)
                    {
                        Debug.LogErrorFormat($"このGUID \"{guid}\" は登録されていません");
                    }
                }
                else
                {
                    Debug.LogErrorFormat("不正なQRコードです。");
                }
            });
        }
        
        public void SetQRValue(string value)
        {
            if (_qrValue.Value != value)
            {
                _qrValue.Value = value;
            }
        }
    }
}