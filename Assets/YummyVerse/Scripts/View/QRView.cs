using Meta.XR.MRUtilityKit;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View
{
    /// <summary>
    /// QRコードを読み取って、QRの値とMR座標系におけるTransformを返す
    /// </summary>
    public class QRView : MonoBehaviour
    {
        private IQRViewModel _qrViewModel;
        
        [Inject]
        public void Construct(IQRViewModel qrViewModel)
        {
            this._qrViewModel = qrViewModel;
        }
        
        public void OnTrackableAdded(MRUKTrackable trackable)
        {
            if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
            {
                _qrViewModel.HandleTrackableAdded(new MRUKTrackableAdapter(trackable));
            }
        }
    }
}
