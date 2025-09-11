using Food3DModel.Interface;
using FoodDB.Scripts.ViewModel;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;
using Zenject;

namespace Food3DModel.View
{
    public class QRView: MonoBehaviour
    {
        [Inject] private IQRViewModel _viewModel;
        
        [SerializeField] TextMeshPro _debugText;
        public void OnTrackableAdded(MRUKTrackable trackable)
        {
            if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
            {
                _debugText.text = trackable.MarkerPayloadString;
                _viewModel.OnDetectQRCode(trackable.MarkerPayloadString,trackable.transform);
            }
        }
    }
}