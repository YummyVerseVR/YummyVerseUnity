# Construction: Implementation Plan and Progress

## Status

`CODE-COMPLETE-PENDING-RUNTIME-VERIFICATION`。Domain/Application、Infrastructure adapter、Presentation coordinator、thin View、composition root、legacy 削除のコード変更が入り、C# compile gate は通過した。Unity Scene/Prefab load、Quest、PCVR の実機・Editor 検証は未実行であり、intent-wide completion は宣言しない。

## Initial Findings (baseline)

- active build scene は `Assets/YummyVerse/Scene/Restaurant.unity` のみである。
- active scene から再帰参照される app script は 34 個で、DI から pure services が追加到達する。
- baseline `dotnet build Assembly-CSharp.csproj --no-restore --nologo` は 0 errors / 25 warnings である。これは redesign 後の結果ではない。
- `FoodSelectionMenuView` は 477 行、`FoodPlacementCubeView` は 269 行、`FoodView` は 219 行、`ConfigUIView` は 219 行である。
- `RestaurantInstaller` には多数の binding がある。
- `FoodInstaller` は空だが `FoodView.prefab` に付与されている。
- `Assets/FoodDB/Scripts` は初期監査上 active graph 非到達で、obsolete WWW 警告を発生させる legacy candidate である。

詳細な根拠と未確定事項は `audit/codex-redesign.md` にある。

## Main Design

```text
Domain contracts/value objects
        -> Application use cases and role-specific ports
        -> Infrastructure adapters / Presentation coordinators
        -> thin Unity View and device/transport boundary
        -> composition root binds concrete implementations
```

- Domain/Application は Unity、View concrete、network/filesystem/PlayerPrefs、Meta XR、glTF、input concrete を知らない。
- Application は session/business decisions、状態遷移、catalog/selection、placement、eating、settings の use case を持つ。
- Network/Standalone、placement/anchor、QR designation、artifact/model、input の port は役割ごとに分ける。
- transport DTO/raw JSON/SDK type は adapter mapper 境界へ閉じ込める。
- View は serialized refs、lifecycle、render/input forwarding、tick forwarding だけを行い、UI generation/I/O/policy/state transition/subscription ownership を持たない。
- Installer は composition root として feature registration に委譲する。
- read-only state/property と command method、subscription/cancellation/disposal owner を feature ごとに定義する。

## Implemented Unit Status

| Unit | Status | 実装された内容 |
| --- | --- | --- |
| `UNIT-AR-01` | `DONE` | active root/GUID/asset graph の再走査。結果は下の「Reachability and Asset Evidence」。 |
| `UNIT-AR-02` | `DONE` | `Model/Struct` の value object と `Model` の pure service（`FoodConsumptionState`、`FoodInteractionBoundsCalculator`、`ScoopContactDetector`、`AppStateMachine`、`FoodContext` 他）。EditMode test 済み。 |
| `UNIT-AR-03` | `DONE` | `Model/Interface` に 27 の role-specific port。`FoodCatalogService`、`FoodLoaderRouter`、`FoodPlacementService`、`FoodEatingService`、`GameCommandHandler` が use case を保持する。 |
| `UNIT-AR-04` | `DONE` | `Scripts/Infrastructure` に 15 adapter。transport DTO は `MenuResponseDto` + `FoodCatalogTransportMapper` に閉じた。 |
| `UNIT-AR-05` | `DONE` | `Scripts/Presentation` に 10 coordinator。4 つの大型 View を lifecycle adapter へ縮小した。 |
| `UNIT-AR-06` | `DONE` | `RestaurantInstaller` は 4 つの feature registration へ委譲。空の `FoodInstaller` は削除し `FoodView.prefab` から detach した。 |
| `UNIT-AR-07` | `DONE` | `Assets/FoodDB/Scripts`、Dummy/Diagnostics/Tests の残骸、空 directory と orphan folder `.meta` を削除した。 |
| `UNIT-AR-08` | `PARTIAL` | `G-01` のみ実行。`G-02`〜`G-06` は未実行。`verification/test-results.md` を参照。 |

## Layer Inventory

| Layer | Path | `.cs` 数 |
| --- | --- | --- |
| Application/Domain service | `Scripts/Model` | 20 |
| Port | `Scripts/Model/Interface` | 27 |
| Value object / DTO | `Scripts/Model/Struct` | 16 |
| Composition root | `Scripts/Model/DI` | 2 |
| Infrastructure adapter | `Scripts/Infrastructure` | 15 |
| Presentation coordinator | `Scripts/Presentation` | 10 |
| Thin Unity View | `Scripts/View` | 5（+ `View/UI`、`View/Tutorial`） |

## View Thinning Result

| View | baseline 行数 | 現在 | 残した責務 |
| --- | --- | --- | --- |
| `FoodSelectionMenuView` | 477 | 50 | `IFoodSelectionMenu` の forwarding と `Update` tick |
| `FoodPlacementCubeView` | 269 | 45 | serialized transform、enable/disable、tick、dispose |
| `FoodView` | 219 | 81 | `ScoopDetectionSettings` の serialized field と stream 購読の forwarding |
| `ConfigUIView` | 219 | 58 | serialized UI reference の受け渡しと lifecycle |

UI 生成は `FoodSelectionMenuUiBuilder`、preview I/O は `FoodPreviewLoader`、placement policy は `FoodPlacementPreviewController`、settings policy は `ConfigUIPresenter` が持つ。`partial MonoBehaviour` や helper MonoBehaviour への責務移動は行っていない。

## Infrastructure Adapter Boundary

`Scripts/Infrastructure` の adapter は port を実装し、`Model` root の Application 型を一切参照しない（依存は `Model.Interface` と `Model.Struct` のみ）。

| Adapter | Port | 境界 |
| --- | --- | --- |
| `NetworkFoodCatalogSource` | `IRemoteFoodCatalogSource` | YummyService v2 HTTP + `FoodCatalogTransportMapper` |
| `PersistentFoodCatalogSource`、`PersistentFoodCatalogScanner` | `IPersistentFoodCatalogSource` | persistentDataPath の file layout |
| `UnityPersistentFoodCatalogPath` | `IPersistentFoodCatalogPath` | `Application.persistentDataPath` |
| `NetworkFoodLoader`、`LocalFoodLoader`、`GltfImportFactory` | `IFoodModelLoader` 系 | UnityWebRequest、file、glTF、SHA-256 |
| `MetaSpatialAnchorBackend` | spatial anchor gateway | `OVRAnchor` / `OVRSpatialAnchor` |
| `PlayerPrefsFoodPlacementStore` | placement store | `PlayerPrefs` |
| `NetworkConnectionTester` | connection tester | `UnityWebRequest` |
| `OvrScoopProbeProvider`、`OvrScoopHaptics` | scoop probe / haptics | `OVRInput`、`OVRCameraRig` |
| `InputLayer` | controller command source | `UnityEngine.InputSystem` |
| `QRValueValidator`、`MRUKTrackableAdapter` | QR designation | MRUK |

## Composition Root

`RestaurantInstaller` は 4 registration へ委譲する。

- `RestaurantCoreBindings`: session/state/event/placement/settings の singleton。
- `RestaurantCatalogBindings`: Network/Standalone catalog source を別 port として bind し、loader router を構成する。
- `RestaurantInteractionBindings`: eating、scoop device、View 用 presenter を bind する。
- `RestaurantCommandBindings`: `GameCommandHandler` を `NonLazy` で activate する（理由をコード内に記録）。

Feature 側の `ConfigUIInstaller`、`TutorialInstaller`、`QRInstaller`、`SharedViewModelInstaller` は Presentation coordinator を `AsTransient` で bind する。空 Installer は残っていない。

## Deletion Evidence

| 削除対象 | class reference | script GUID | active graph | tests/editor tooling |
| --- | --- | --- | --- | --- |
| `Assets/FoodDB/Scripts` 全 script | code 参照 0 | 削除 33 GUID を `Assets`/`ProjectSettings` 全走査して被参照 0 | active scene/prefab 非到達 | 参照なし |
| `DummyFoodFetchable`、`DummyQRView`、`DummyGameEventView`、`DummyQRTrackable` | code 参照 0 | 被参照 0（`DummyQRView.prefab` も削除） | 非到達 | 参照なし |
| `PcvrLinkDiagnostics`、`QRFlowTest`、`FollowHand`、`CountedEventCondition` | code 参照 0 | 被参照 0 | 非到達 | 参照なし |
| 空 directory と folder `.meta`（`FoodDB/Scripts` 配下 6 + `Scripts.meta`、`Diagnostics`、`Model/Dummies`、`Model/Dummies/Struct`、`View/Dummies`、`Tests`） | 該当なし | folder GUID 12 件を全走査して被参照 0 | 非到達 | 参照なし |

`FoodInstaller` は削除し、`FoodView.prefab` の `GameObjectContext._monoInstallers` を空へ更新した。`GameObjectContext` 自体は prefab 単位の injection scope として残す。

## Reachability and Asset Evidence

再走査結果:

- `Restaurant.unity` の `m_Script` GUID: 未解決 0。
- `Assets/YummyVerse/Prefabs` 配下 prefab の `m_Script` GUID: 未解決 0。
- `Assets/YummyVerse` と `Assets/FoodDB` の orphan `.meta`（対応 asset なし）: 0。
- `m_EditorClassIdentifier` の型解決: `Assembly-CSharp` の 36 型すべて解決。ただし `Food3DModel.View.QRreader` が `Assets/Scenes/QRTrackMR.unity`、`Assets/_Recovery/0.unity`、`Assets/_Recovery/0 (1).unity`、`Assets/FoodDB/Prefabs/CameraVideo.prefab` に残る。これは本 intent 以前から欠落している missing script であり、enabled build scene には含まれない。本 intent では削除対象にしない。

## Not Yet Claimed

- Unity Editor による Scene/Prefab load、DI container validation、`NonLazy`/`IInitializable` の実行時 activation。
- EditMode test 64 件の実行結果。
- Quest/Android/OpenXR および PCVR/Editor の動作。
- settings UI、controller interaction、VR display ordering、Tutorial、Standalone、YummyService v2 の回帰解消。

## Known Remaining Design Gap

`Model/Struct/Food.cs` は `GLTFast.GltfImport` を保持しており、`IFoodViewModel.foodGltf` から `FoodView` まで glTF SDK 型が Domain/Application を通過する。これを除去するには model handle abstraction を loader/router/ViewModel/View の連鎖に導入する必要があり、本 intent の実装では着手していない。`NFR-AR-001` の残課題として記録する。
