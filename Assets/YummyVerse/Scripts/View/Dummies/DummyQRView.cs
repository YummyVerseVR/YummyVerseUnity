using Meta.XR.MRUtilityKit;
using UnityEngine;
using YummyVerse.Scripts.Model.Dummies.Struct;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Dummies
{
    /// <summary>
    /// VRなしでとりあえず読み込んだことにできるようにするスクリプト
    /// </summary>
    public class DummyQRView : MonoBehaviour
    {
        private IQRViewModel _qrViewModel;
        
        [Inject]
        public void Construct(IQRViewModel qrViewModel)
        {
            this._qrViewModel = qrViewModel;
        }

        public void FakeQRDetect()
        {
            var dummyTrackable = new DummyQRTrackable(this.transform, "f92a4057-f5c7-43d5-ab0f-e6af3592b3af");
            _qrViewModel.HandleTrackableAdded(dummyTrackable);
        }
    }
}