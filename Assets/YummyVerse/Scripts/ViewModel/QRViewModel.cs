using System;
using Food3DModel.Interface;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using IQRViewModel = YummyVerse.Scripts.ViewModel.Interface.IQRViewModel;

namespace YummyVerse.Scripts.ViewModel
{
    public class QRViewModel : IQRViewModel
    {
        private readonly IQRDetectionService _qrDetectionService;
        private readonly IQRValueValidator _qrValueValidator;

        public QRViewModel(IQRValueValidator qrValueValidator, IQRDetectionService qrDetectionService)
        {
            _qrValueValidator = qrValueValidator;
            _qrDetectionService = qrDetectionService;
        }
        
        
        /// <summary>
        /// QR読み取り時に呼び出す
        /// </summary>
        /// <param name="trackable">追尾対象の物体(QRコードを想定</param>
        /// <exception cref="NotImplementedException"></exception>
        public void HandleTrackableAdded(MRUKTrackable trackable)
        {
            var qrStr = trackable.MarkerPayloadString; // QRコードの値
            var transform = trackable.transform; // QRのMR座標系におけるTransform
            var validationResult = _qrValueValidator.Validate(qrStr);
            if (validationResult.IsValid)
            {
                _qrDetectionService.NotifyDetectQR(validationResult.Guid, transform);
            }
        }
    }
}