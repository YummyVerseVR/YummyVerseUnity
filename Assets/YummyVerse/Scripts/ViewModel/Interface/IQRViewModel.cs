using Meta.XR.MRUtilityKit;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IQRViewModel
    {
        void HandleTrackableAdded(IQRTrackable trackable);
    }
}