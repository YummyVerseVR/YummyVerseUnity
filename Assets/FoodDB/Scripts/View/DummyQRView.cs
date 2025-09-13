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
            _viewModel.OnDetectQRCode("7b998836-903e-4878-ae8e-839a2ef13373", this.transform);
        }
    }
}