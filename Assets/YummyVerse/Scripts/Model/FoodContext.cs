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
        private IFoodFetchable _foodFetchable;
        private readonly IQRDetectionService _qrDetectionService;
        private readonly IFoodFetchableFactory _foodFetchableFactory;
        
        private readonly CompositeDisposable _disposables  = new CompositeDisposable();
        
        public ReactiveProperty<FoodDownloadResult> downloadResult { get; } = new ();
        
        public FoodContext(IQRDetectionService qrDetectionService,  IFoodFetchableFactory foodFetchableFactory)
        {
            this._qrDetectionService = qrDetectionService;
            this._foodFetchableFactory = foodFetchableFactory;
        }

        public void Initialize()
        {
            // QRコードに映っているGuidが更新されたらダウンロードを開始する。
            _qrDetectionService.OnChangeGUID.Where(v => v != Guid.Empty).SubscribeAwait(async (v, ct) =>
            {
                _foodFetchable = _foodFetchableFactory.Create();
                downloadResult.Value = await _foodFetchable.Download(v, ct);
            }).AddTo(_disposables);
        }
        
        public void Dispose()
        {
            downloadResult?.Dispose();
            _disposables?.Dispose();
        }
    }
}