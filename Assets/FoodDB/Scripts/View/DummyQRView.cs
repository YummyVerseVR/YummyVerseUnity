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
            _viewModel.OnDetectQRCode("fd252c3c-fdf7-419d-a24c-87f94dc626df", this.transform);
        }
    }
}