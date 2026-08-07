using System;
using Meta.XR.MRUtilityKit;
using TMPro;
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
                _qrViewModel.HandleTrackableAdded(new MRUKTrackableAdapter(trackable.transform, trackable.MarkerPayloadString));
            }
        }

        /// <summary>
        /// MRUK の TrackableRemoved から呼ばれる。プレハブ側の UnityEvent に接続すること。
        /// </summary>
        public void OnTrackableRemoved(MRUKTrackable trackable)
        {
            if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
            {
                _qrViewModel.HandleTrackableRemoved(new MRUKTrackableAdapter(trackable.transform, trackable.MarkerPayloadString));
            }
        }
    }
}
