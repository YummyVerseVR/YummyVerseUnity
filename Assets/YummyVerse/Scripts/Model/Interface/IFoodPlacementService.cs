using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodPlacementService
    {
        ReactiveProperty<Transform> FoodTransform { get; }
        ReactiveProperty<FoodPlacementState> State { get; }
        ReactiveProperty<string> StatusMessage { get; }
        ReactiveProperty<bool> IsAnchorReady { get; }
        ReactiveProperty<bool> IsFoodPositionFixed { get; }
        ReactiveProperty<bool> IsConfigurationVisible { get; }
        ReactiveProperty<bool> IsBusy { get; }

        /// <summary>
        /// 食べ物の表示位置が使える状態かどうか。
        /// 保存済み設定の復元中・復元済み、または設定画面で配置モデルを置いた場合に true。
        /// false のまま食べ物を出すと、表示位置が無いため画面に何も現れない。
        /// </summary>
        ReactiveProperty<bool> IsPlacementConfigured { get; }

        void SetConfigurationVisible(bool isVisible);
        void UpdateDraftPose(Pose pose);
        bool TryActivateDraftPoseForFood();
        bool TryGetSuggestedDraftPose(out Pose pose);
        UniTask<bool> SetAnchorAtDraftAsync(CancellationToken cancellationToken);
        UniTask<bool> FixFoodPositionAtDraftAsync(CancellationToken cancellationToken);
    }
}
