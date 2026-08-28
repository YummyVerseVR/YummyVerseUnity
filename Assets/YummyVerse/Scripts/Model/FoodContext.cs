using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    public class FoodContext : IFoodContext, IInitializable, IDisposable
    {
        private readonly IGameEventBus _gameEventBus;
        private readonly IFoodModelLoader _foodModelLoader;
        private readonly CancellationTokenSource _lifetimeCancellation = new();
        
        private readonly CompositeDisposable _disposables  = new CompositeDisposable();
        private CancellationTokenSource _selectionCancellation;
        private int _selectionVersion;
        private bool _isDisposed;
        
        public ReactiveProperty<FoodDownloadResult> downloadResult { get; } = new ();
        
        public FoodContext(IGameEventBus gameEventBus, IFoodModelLoader foodModelLoader)
        {
            _gameEventBus = gameEventBus ?? throw new ArgumentNullException(nameof(gameEventBus));
            _foodModelLoader = foodModelLoader ?? throw new ArgumentNullException(nameof(foodModelLoader));
        }

        public void Initialize()
        {
            // 食品 identity はメニュー選択からのみ受け取る。
            // 物理 QR の payload/GUID は anchor designation の入力であり、食品 load の起点にしない。
            Observable.FromEvent<MenuItem>(
                    h => _gameEventBus.OnMenuItemSelected += h,
                    h => _gameEventBus.OnMenuItemSelected -= h)
                .Where(item => item.IsValid)
                .Subscribe(StartLoad).AddTo(_disposables);
        }
        
        public void Reset()
        {
            CancelSelection();
            _selectionVersion++;
            downloadResult.Value = default;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            CancelSelection();
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
            downloadResult?.Dispose();
            _disposables?.Dispose();
        }

        private void StartLoad(MenuItem item)
        {
            if (_isDisposed) return;

            CancelSelection();
            var version = ++_selectionVersion;
            _selectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCancellation.Token);
            LoadAsync(item, version, _selectionCancellation.Token).Forget();
        }

        private async UniTaskVoid LoadAsync(
            MenuItem item,
            int version,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _foodModelLoader.LoadAsync(item, cancellationToken);
                if (!_isDisposed && version == _selectionVersion && !cancellationToken.IsCancellationRequested)
                {
                    downloadResult.Value = result;
                }
            }
            catch (OperationCanceledException)
            {
                // A newer selection or the owning scene cancelled this request.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void CancelSelection()
        {
            if (_selectionCancellation == null) return;
            _selectionCancellation.Cancel();
            _selectionCancellation.Dispose();
            _selectionCancellation = null;
        }
    }
}
