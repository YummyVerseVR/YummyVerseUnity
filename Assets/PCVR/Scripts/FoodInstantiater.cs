using PCVR.Model.Interfaces;
using UnityEngine;
using Zenject;

namespace PCVR
{
    public class FoodInstantiater : MonoBehaviour
    {
        [Inject] private IQRCodeManager QRCodeManager;
        void Start()
        {
        
        }
    }
}
