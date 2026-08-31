# Construction Implementation

## Implemented Scope

- QR の GUID stream は食べ物選択に残し、`FoodViewModel` の pose source を `IFoodPlacementService` へ変更した。
- `OVRSpatialAnchor` の create/save/load/localize/bind/erase を `MetaSpatialAnchorBackend` に隔離した。
- schema version、Anchor UUID、anchor-relative position/rotation、確定状態を単一 JSON record として PlayerPrefs に保存する。
- 設定画面へ `Set / Update Spatial Anchor`、`Lock Food Position`、状態表示を追加した。
- 設定画面表示中だけ、既存 Meta controller GrabInteractor から操作できる world-space Cube を表示する。
- Anchor と Cube を別 GameObject にし、`Lock Food Position` 時に `InverseTransformPoint` と inverse rotation で relative pose を確定する。
- Anchor 未設定、復元中、復元失敗時は食品 root を非表示にして world origin への誤表示を防止する。
- Standalone Mode は food GUID だけを切り替え、QR Transform と皿検出イベントを更新しない。
- 設定 UI prefab の scene instance を常時 active にし、表示と raycast は既存 CanvasGroup で制御する。

## Replacement Transaction

Anchor 再設定では、`Set / Update Spatial Anchor` で新 Anchor を作成・端末保存しても、旧 Anchor と旧食品表示は直ちに削除しない。`Lock Food Position` で新 UUID と relative pose の一レコード保存に成功した後に新設定を適用し、最後に旧 Anchor を erase する。Anchor 作成・保存または pose record 保存に失敗した場合は pending Anchor を破棄し、利用可能な旧設定へ rollback する。

## Main Files

- `Assets/YummyVerse/Scripts/Model/FoodPlacementService.cs`
- `Assets/YummyVerse/Scripts/Model/MetaSpatialAnchorBackend.cs`
- `Assets/YummyVerse/Scripts/Model/PlayerPrefsFoodPlacementStore.cs`
- `Assets/YummyVerse/Scripts/View/FoodPlacementCubeView.cs`
- `Assets/YummyVerse/Scripts/View/FoodView.cs`
- `Assets/YummyVerse/Scripts/ViewModel/FoodViewModel.cs`
- `Assets/YummyVerse/Scripts/View/UI/ConfigUIView.cs`
- `Assets/YummyVerse/Scripts/ViewModel/UI/ConfigUIViewModel.cs`
- `Assets/YummyVerse/Prefabs/Restaurant/UI/YummyConfigUI.prefab`
- `Assets/YummyVerse/Scene/Restaurant.unity`

## Deferred Verification

Quest runtime と物理空間を必要とする create/save/load/localization、再起動復元、実コントローラー Grip は Editor では合格判定しない。手順と未検証項目は `verification/test-results.md` に記録する。
