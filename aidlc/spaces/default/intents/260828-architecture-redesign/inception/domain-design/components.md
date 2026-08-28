# Domain Components

## Design Goal

active runtime の挙動を維持しながら、core の不変条件・use case・外部 adapter・Presentation・Unity View・composition root を分離する。責務の境界はディレクトリ名ではなく、依存方向、入力/出力、lifecycle owner、テスト可能性で判定する。

## Target Dependency Model

```text
Domain contracts/value objects
              ^
              |
Application use cases ---- Application role-specific ports
              ^                         ^
              |                         |
Presentation/ViewModel           Infrastructure adapters
              ^                         ^
              |                         |
       thin Unity View       SDK/HTTP/file/device concrete

Composition root -> all concrete registrations and scopes
```

実際の参照方向は「consumer が contract を参照し、adapter が port を実装する」である。新しい reverse dependency は作らない。composition root 以外で concrete を `new`/bind しない。

## Runtime Roots and Evidence

| Component | Responsibility | Root/evidence |
| --- | --- | --- |
| `C-AR-01 ActiveRootAudit` | enabled Scene、再帰 asset graph、DI activation、callback、Editor test root の列挙 | `ProjectSettings/EditorBuildSettings.asset`、`Restaurant.unity`、Installer、Editor tests |
| `C-AR-10 DeletionAndAssetGate` | 削除候補の参照・GUID・asset graph・test evidence と変更前後検証 | intent audit、`.meta`、Scene/Prefab/ScriptableObject/UnityEvent |
| `C-AR-11 VerificationMatrix` | EditMode、contract、Unity load、Quest、PCVR/Editor の結果分離 | `verification/` |

## Core Components

### C-AR-02 Domain Core

責務:

- food item/source/placement/session の identity と immutable value object。
- portion、selection、session state などの不変条件と純粋な状態遷移。
- source-specific result/error の意味を保持できる domain contract。

依存:

- Domain 内の contract/value object のみ。
- UnityEngine、MonoBehaviour、transport DTO、raw JSON、SDK type、filesystem、PlayerPrefs、Meta XR、glTF、input concrete への依存なし。

テスト:

- EditMode unit test。GameObject、Scene、device、network を必要としない。

### C-AR-03 Application Use Cases

責務:

- session start/reset、Tutorial flow、catalog/selection、placement、food interaction、settings command の業務判断。
- Domain を組み合わせ、必要な role-specific port を呼ぶ。
- read-only state/result と command の境界、request generation/stale response の policy。

依存:

- C-AR-02 Domain と C-AR-04 Application port。
- adapter/Unity/View concrete への依存なし。

代表的な use case（target role 名）:

| Use case | 入力 command | 出力/read-only state | 使用 port |
| --- | --- | --- | --- |
| Session lifecycle | `StartSession`、`ResetSession` | app/session state | clock、event publisher、reset collaborators |
| Food selection | `SelectFood` | catalog/selection state | Network catalog、Standalone catalog、model loader |
| Placement configuration | `Open/ConfirmPlacement` | placement state | anchor gateway、placement store |
| Settings | `Edit/ConfirmSettings` | settings state/result | settings store、connection tester |
| Food consumption | `Scoop`/tick | portion/eating state | scoop probe、haptics/effect port |

### C-AR-04 Application Role-Specific Ports

port は Application の consumer が必要とする最小契約として置く。実装型や transport DTO を公開しない。

| Port | Consumer | Adapter candidates | 差異として保持するもの |
| --- | --- | --- | --- |
| `INetworkFoodCatalogReader` | network catalog use case | YummyService v2 adapter | order/artifact identity、HTTP/contract failure |
| `IStandaloneFoodCatalogReader` | Standalone catalog use case | persistent local adapter | local namespace、missing/corrupt file |
| `ISelectedArtifactReader` | selected model/preview use case | v2 artifact adapter、local artifact adapter | preview/GLB policy、revision/SHA, cancel |
| `IFoodPlacementStore` | placement use case | PlayerPrefs/file adapter | stored UUID、anchor-relative pose、persistence failure |
| `ISpatialAnchorGateway` | placement use case | Meta XR adapter | create/save/load/localize/provider failure |
| `IControllerCommandSource` | input/application | XR input adapter | controller lifecycle/button state |
| `IQrDesignationSource` | placement | QR/MRUK adapter | designation/lost; food identity には使わない |
| `IFoodModelLoader` | food presentation | glTF/local/network loader adapter | bytes/parse/instantiate/cancel |
| `IScoopProbe`、`IScoopHaptics` | eating use case | device adapters | probe/haptic availability |

Network/Standalone を一つの `IFetchable` にまとめない。collection bind が必要な場合は source selector が明示的に source identity を選び、各 adapter の failure を保持する。

### C-AR-05 Infrastructure Adapters

責務:

- HTTP、YummyService v2 transport、filesystem、PlayerPrefs、Meta XR、MRUK/QR、glTF、XR input、clock の concrete access。
- DTO/raw JSON/SDK result を mapper で C-AR-02/C-AR-03 の contract/result へ変換。
- timeout、retry、cancellation、checksum、missing asset、provider failure を境界ごとに処理。

禁止:

- View concrete、UI tree、Application policy の呼び出し。
- v1/legacy route を v2 の代替として使うこと。
- Network failure を Standalone failure に変換すること。

### C-AR-06 Application Coordinator / Presentation ViewModel

責務:

- use case の state を read-only presentation state へ mapper する。
- button/touch/controller callback を command method へ変換する。
- message、loading、error、retry、visibility、selection の表示 policy。
- subscription/cancellation/disposal の owner と boundary を明記する。

UI tree の生成、HTTP/file I/O、model instantiate、session state transition は持たない。

### C-AR-07 Thin Unity View

許可:

- `[SerializeField]` reference と `Awake`/`OnEnable`/`OnDisable`/`OnDestroy`。
- Unity UI/renderer/input callback の collaborator への forwarding。
- read-only state の text/image/visibility/render 反映。
- `Update`/`LateUpdate` の tick forwarding。

禁止:

- UI/menu の動的生成、catalog policy、I/O、download/decode/glTF、state transition、long switch、subscription owner の判断。
- `partial MonoBehaviour` または helper MonoBehaviour へ同じ業務責務を移すこと。

対象となる現行 View:

| Current View | target extraction lens |
| --- | --- |
| `FoodSelectionMenuView` | menu rendering/input forwarding と catalog/selection policy を分離 |
| `FoodPlacementCubeView` | grab/render forwarding と placement use case/lifecycle を分離 |
| `FoodView` | model/effect rendering と loading/eating state を分離 |
| `ConfigUIView` | serialized UI/render forwarding と settings/validation/overlay state を分離 |

既存の controller interaction、VR 空間上の表示順、serialized field、Prefab の見た目・操作は、抽出後に regression gate として確認する。

### C-AR-08 Composition Root

`RestaurantInstaller` と feature registration が担当する。

- role-specific port と concrete adapter の bind。
- use case/ViewModel/Presenter の scope。
- `NonLazy`/`IInitializable` の activate 理由と順序。
- Scene/Prefab の serialized component を必要な boundary へ接続する構成。
- 空 Installer、implicit global lookup、曖昧な multi-bind の排除。

Installer は feature registration へ委譲する。Scene に置かれていても registration が空なら active behavior の根拠として扱わず、削除/統合の候補にする。

### C-AR-09 Lifecycle Registry

各 use case/ViewModel/adapter について、subscription、UniTask、R3 stream、temporary effect の owner を記録する。

| Resource | owner | cancellation/disposal |
| --- | --- | --- |
| session task | session orchestrator | reset、absence、fatal rescue |
| View subscription | ViewModel/View scope | `OnDisable`、`OnDestroy` |
| Scene resident stream | feature composition root | scope/application shutdown |
| network/local request | calling use case | caller token、timeout、source disposal |
| effect object | effect coordinator | reset、completion、destroy |

cancel は成功完了に変換しない。reset cleanup は中断 token に巻き込まれない。再入場で前 session の stream が二重購読されない。

## Feature Flow

```text
Unity callback/input
        |
        v
Thin View / device adapter --command--> Presentation coordinator
                                              |
                                              v
                                      Application use case
                                       /             \
                                      v               v
                                  Domain       role-specific port
                                                      |
                                                      v
                                               Infrastructure adapter
```

食べ物選択では Catalog use case が Network/Standalone source を source-aware item に集約するが、各 source の identity/failure/lifecycle を失わない。設定では View が `Open/Confirm/Cancel` を command として forwarding し、placement/settings policy は Application に置く。

## Existing-to-Target Migration Map

| Current area | 初期監査での扱い | target migration |
| --- | --- | --- |
| `Assets/YummyVerse/Scripts/Model` | pure domain と service/device concrete が混在している可能性 | C-AR-02〜05 の責務ごとに分割。名前だけの移動で済ませない |
| `Assets/YummyVerse/Scripts/ViewModel` | use case、Presenter、subscription が混在している可能性 | C-AR-03/C-AR-06 へ分類し、port を consumer 側に置く |
| `Assets/YummyVerse/Scripts/View` | UI と業務処理が混在する大きな View がある | C-AR-07 と C-AR-06/C-AR-03 へ抽出 |
| `Assets/YummyVerse/Scripts/Model/DI/RestaurantInstaller.cs` | 多数 binding の composition root 候補 | C-AR-08 の feature registration へ整理 |
| `Assets/YummyVerse/Scripts/ViewModel/DI/FoodInstaller.cs` | 空 Installer だが `FoodView.prefab` に付与 | serialized GUID/graph を確認後、統合または削除を判断 |
| `Assets/FoodDB/Scripts` | active graph 非到達の legacy 候補 | C-AR-10 の証拠が揃うまで削除済みとしない |

## Component Completion Gate

- 各 component に consumer、port、concrete adapter、owner、test seam がある。
- dependency scan で reverse dependency、Unity concrete leakage、generic multi-bind がない。
- Scene/Prefab/asset 変更は GUID と Unity load gate を通る。
- 実装・削除・テスト結果は別の証拠として construction/verification に追記する。
