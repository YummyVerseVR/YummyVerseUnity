using System;
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
        public void HandleTrackableAdded(IQRTrackable trackable)
        {
            var qrStr = trackable.qrPayload; // QRコードの値
            var transform = trackable.transform; // QRのTransformを参照で保持して追従させる
            var validationResult = _qrValueValidator.Validate(qrStr);
            if (validationResult.IsValid)
            {
                _qrDetectionService.NotifyDetectQR(validationResult.Guid, transform);
            }
        }

        /// <summary>
        /// QRの追尾が外れたときに呼び出す。
        /// 妥当なQRだったものがロストしたときだけ通知する。
        /// </summary>
        /// <param name="trackable">追尾が外れた物体</param>
        public void HandleTrackableRemoved(IQRTrackable trackable)
        {
            var validationResult = _qrValueValidator.Validate(trackable.qrPayload);
            if (validationResult.IsValid)
            {
                _qrDetectionService.NotifyLostQR();
            }
        }
    }
}
