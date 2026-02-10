using FoodDB.Scripts.ViewModel;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using Zenject;

namespace Food3DModel.View
{
    public class DummyQRView: MonoBehaviour
    {
        [Inject] private QRViewModel _viewModel;
        public void OnTrackableAdded()
        {
            _viewModel.OnDetectQRCode("cef7509e-e99d-4482-ae49-7a3b9f4b6668", this.transform);
        }
    }
}