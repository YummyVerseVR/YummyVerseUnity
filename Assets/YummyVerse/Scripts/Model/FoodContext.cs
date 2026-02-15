using System;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using R3;
using UnityEngine;
using UnityEngine.Networking;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    public class FoodContext : IFoodContext, IInitializable, IDisposable
    {
        private readonly IFoodDownloader _foodDownloader;
        private readonly IQRDetectionService _qrDetectionService;
        
        private CompositeDisposable _disposables  = new CompositeDisposable();
        
        public ReactiveProperty<FoodDownloadResult> downloadResult { get; private set; } = new ();
        
        public FoodContext(IFoodDownloader downloader, IQRDetectionService qrDetectionService)
        {
            this._foodDownloader = downloader;
            this._qrDetectionService = qrDetectionService;
        }

        public void Initialize()
        {
            // QRコードに映っているGuidが更新されたらダウンロードを開始する。
            _qrDetectionService.OnChangeGUID.Where(v => v != Guid.Empty).SubscribeAwait(async (v, ct) =>
            { 
                downloadResult.Value = await _foodDownloader.Download(v, ct);
            }).AddTo(_disposables);
        }
        
        public void Dispose()
        {
            downloadResult?.Dispose();
            _disposables?.Dispose();
        }
    }
}