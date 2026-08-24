using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    public class FoodContext : IFoodContext, IInitializable, IDisposable
    {
        private IFoodFetchable _foodFetchable;
        private readonly IGameEventBus _gameEventBus;
        private readonly IFoodFetchableFactory _foodFetchableFactory;
        
        private readonly CompositeDisposable _disposables  = new CompositeDisposable();
        
        public ReactiveProperty<FoodDownloadResult> downloadResult { get; } = new ();
        
        public FoodContext(IGameEventBus gameEventBus, IFoodFetchableFactory foodFetchableFactory)
        {
            _gameEventBus = gameEventBus;
            _foodFetchableFactory = foodFetchableFactory;
        }

        public void Initialize()
        {
            // 食品 identity はメニュー選択からのみ受け取る。
            // 物理 QR の payload/GUID は anchor designation の入力であり、食品 load の起点にしない。
            Observable.FromEvent<MenuItem>(
                    h => _gameEventBus.OnMenuItemSelected += h,
                    h => _gameEventBus.OnMenuItemSelected -= h)
                .Where(item => item.Guid != Guid.Empty)
                .SubscribeAwait(async (item, ct) =>
                {
                    _foodFetchable = _foodFetchableFactory.Create();
                    downloadResult.Value = await _foodFetchable.Download(item.Guid, ct);
                }).AddTo(_disposables);
        }
        
        public void Reset()
        {
            downloadResult.Value = default;
        }

        public void Dispose()
        {
            downloadResult?.Dispose();
            _disposables?.Dispose();
        }
    }
}
