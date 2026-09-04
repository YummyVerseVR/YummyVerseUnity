using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.ViewModel.Interface;

namespace YummyVerse.Scripts.ViewModel
{
    /// <summary>チュートリアル終了後の catalog 読込、選択、game event 発行をつなぐ。</summary>
    public sealed class FoodSelectionFlow : IFoodSelectionFlow
    {
        private readonly IFoodCatalogService _catalogService;
        private readonly IFoodSelectionMenu _menu;
        private readonly IGameEventPublisher _eventPublisher;
        private readonly IFoodContext _foodContext;
        private readonly IFoodPlacementService _foodPlacementService;

        public FoodSelectionFlow(
            IFoodCatalogService catalogService,
            IFoodSelectionMenu menu,
            IGameEventPublisher eventPublisher,
            IFoodContext foodContext,
            IFoodPlacementService foodPlacementService)
        {
            _catalogService = catalogService;
            _menu = menu;
            _eventPublisher = eventPublisher;
            _foodContext = foodContext;
            _foodPlacementService = foodPlacementService;
        }

        public async UniTask RunAsync(CancellationToken ct)
        {
            // チュートリアルの ServeRandomPersistentFood と同じく、表示先の Transform を必ず用意する。
            // これが無いと FoodWorldAnchor が非アクティブのままで、ドームも食べ物も一切見えない。
            if (!_foodPlacementService.TryActivateDraftPoseForFood())
            {
                Debug.LogWarning(
                    "[FreePlay] 食べ物の表示位置がありません。"
                    + "管理者用メニューで食品の出現位置を設定してください。");
            }

            // 選択画面に入った時点から食べ物が届くまでは、置き場所にフードドームを被せる。
            _foodContext.BeginPreparation();
            _menu.ShowLoading();
            try
            {
                var catalog = await _catalogService.LoadAsync(ct);
                var selected = await _menu.SelectAsync(catalog, ct);
                _eventPublisher.PublishMenuItemSelected(new MenuItem(selected));
            }
            finally
            {
                _menu.Hide();
            }
        }
    }
}
