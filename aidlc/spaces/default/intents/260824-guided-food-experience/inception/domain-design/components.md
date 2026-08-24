# Domain Components

## Design Goal

来場者体験を `Session orchestration`、`Food catalog/selection`、`Anchor designation/placement`、`Model delivery`、`Eating interaction`、`Presentation` に分離する。食品 identity は仮想メニューの履歴 item から、表示 pose は QR で指定した anchor/永続 placement から得る。チュートリアルは各機能をイベントで観測し、command boundary で依頼するだけにする。

## Existing Implementation Baseline

次は移管元利用ガイドと repository inventory で確認できた現行境界であり、新要件への適合を主張するものではない。

- Session: `AppStateMachine`、`SessionController`、`TutorialRunner`、`FreePlayFlow`。
- Data-driven tutorial: `TutorialSequence`、`TutorialStep`、Narration/Task/Choice、5種類の Condition、`TutorialConfig`、Localization assets。
- Events/commands: `GameEventBus`、`IGameEventPublisher`、`GameCommandBus`、`GameCommandRouter`、`MenuSelectionBridge`。
- Presentation: Message/Hint/Feedback/Choice/Voice Presenter と対応 View、`TutorialDebugHudView`。
- Reset: `GameResetter` と session cancellation。
- Placement: `FoodPlacementService`、`PlayerPrefsFoodPlacementStore`、Meta Spatial Anchor backend、設定用 Cube。
- Current API: `FoodDownloader` が旧 `/{guid}/model` を GET し、`FoodContext` が QR GUID 変更で download を開始する。`IFoodFetchable`/`FoodDownloadResult` も Guid identity に固定されている。
- 現行差分: QR GUID を食品選択へ使う経路、YummyService v2 order/artifact adapter、生成履歴/画像 preview/外部 viewer、実 AABB scoop detector、縮小/crumb/消滅は本 intent の Construction 前に適合確認が必要である。

## Target Component Responsibilities

### Session Orchestrator

- `Attract → Tutorial → FreePlay → Outro → Attract` を管理する。
- Start、user absent、global idle、staff reset、fatal rescue を session lifecycle へ変換する。
- session token を Tutorial、menu/model load、food interaction へ伝播する。
- reset 時に一時状態だけを初期化し、生成履歴と展示 anchor/placement を保持する。

### Tutorial Definition and Runner

- ScriptableObject step sequence を順番に実行し、repeat skip と Choice sub-sequence を扱う。
- Step の enter/exit/elapsed を記録し、現在 step ID を公開する。
- Narration/Task/Choice の3種類と再利用可能 Condition だけで進行する。
- Game event を観測し、必要な依頼は command bus へ発行する。Game/View の具象を参照しない。

### Tutorial Presenters

- Message、Hint、Feedback、Voice、Choice の状態と非同期表示を View から分離する。
- LocalizedString、hint video、success asset を受け取る。
- Scene-level DI scope で解決し、同じ Restaurant scene 内で Tutorial/FreePlay を継続する。

### Game Event and Command Boundary

- Game events: Start、AnchorDesignated/DesignationLost、FoodScooped、DishCleared、MenuItemSelected、UserAbsent。
- Game commands: ServeAppetizer、ServeSelectedItem、ShowMenu、HideMenu、DestroyAllFood。
- Event publisher は成立事実を一度だけ発行し、subscriber の存在や Tutorial state を知らない。
- Command router は既存 ViewModel/service への具象委譲を一箇所に集約する。

### Generated Food Catalog

- `GeneratedFoodItem` を安定した ID と状態で保持する。
- session より長い履歴の列挙、lookup、利用可能性、削除/無効化を提供する。
- preview image reference と model data reference を別フィールド/資源として扱う。
- VR menu と Physical Menu Viewer が同じ item contract を読む。
- Network adapter は YummyService v2 order identity/state を catalog item へ変換し、Standalone adapter は local item を同じ application model へ変換する。
- Network/Standalone の両 source を一つの snapshot/list へ集約するが、source-specific identity と availability を保持する。

```text
GeneratedFoodItem
├── itemId: stable identifier
├── displayName: localized/display metadata
├── previewImageRef: lightweight preview
├── modelDataRef: GLB/local/cache reference
├── status: Generating | Ready | Failed | Unavailable
└── timestamps/optional provenance
```

### Preview Repository

- menu 一覧用 image/metadata の取得、cache、placeholder、retry を担当する。
- model data の download/parse/instantiate を行わない。
- item ごとの失敗を隔離し、一覧全体を止めない。
- Network mode は customer-visible selected `SOURCE_IMAGE_NORMALIZED` artifact revision を候補とし、SHA-256 検証後に cache publish する。正確な API response/visibility は contract publication を待つ。

### Virtual Menu

- Catalog の Ready item を VR 内に一覧表示し、controller selection を item ID として発行する。
- menu/list、selected、loading、ready、error/retry を表現する。
- 食品 model の load や anchor pose 計算を直接行わない。
- Tutorial 完了後の FreePlay で一つだけ表示し、YummyService v2 item と Standalone local item を同時に並べる。
- Item ごとに source を識別可能にし、Network failure/offline でも Standalone item と local selection を維持する。

### Standalone Catalog and Loader Adapter

- 端末内に保存された local 3D model/catalog を列挙し、Network item と同じ `GeneratedFoodItem` application model へ変換する。
- Source namespace を `Standalone` として保持し、Network order/artifact ID と衝突させない。
- Local file の存在、形式、読込可否を item 単位で検証し、破損 item だけを unavailable にする。
- Selection 後は API request を行わず local loader で model を開き、共通の anchor/interaction/consumption flow へ渡す。
- API connection/auth/contract state と独立して利用可能であり、session reset で local asset/catalog を削除しない。

### Physical Menu Viewer

- iPad 等へ catalog item と preview/状態を read-only で提示する。
- VR menu と item identity/status semantics を共有する。
- transport、認証、3D 表示の有無は `Q2` 解決後に adapter として確定する。

### YummyService v2 Contract Guard

- Adopted repository commit、OpenAPI version/checksum、deployed API compatibility を照合する。
- v1 response、未知 enum、missing required field、wrong artifact type を application success へ変換せず fail closed にする。
- `security: []` を anonymous production permission と解釈せず、published auth contract がない環境を production-ready にしない。
- Contract mismatch を secret を含まない診断 event として公開する。

### YummyService v2 Client

- History、order detail/status、selected artifact metadata、preview/GLB download、ProblemDetails を専用 transport boundary に隔離する。
- HTTP DTO を `GeneratedFoodItem`、Order/Stage status、`ArtifactRef` へ mapping し、UI/loader が raw JSON/route を知らないようにする。
- Cancellation、timeout、retry/backoff、stale response suppression を request ごとに適用する。
- v2 normative OpenAPI に path/security/response が追加されるまで production implementation を placeholder URL や v1 route へ接続しない。

```text
ArtifactRef
├── artifactId: opaque string
├── artifactType: SOURCE_IMAGE_NORMALIZED | GLB | WAV | ...
├── revision: opaque non-empty string
├── sha256: 64 hex characters
└── verified: bool
```

### YummyService v2 Artifact Store Adapter

- Selected immutable artifact metadata と binary transfer を分離する。
- Artifact ID/revision/SHA-256 を cache identity にし、一意 temp path へ stream download/incremental hash 後、atomic に cache publish する。
- Preview と GLB の queue/cache/budget を分離し、menu open が GLB fetch を開始しないことを保証する。
- SHA mismatch、truncation、cancel、wrong media/type を隔離して retry 可能にする。

### Anchor Designation and Placement

- QR recognition を anchor designation の入力に限定し、food identity を生成しない。
- 使用可能な anchor/placement pose を公開し、未設定・復元中・失敗を区別する。
- 既存の永続 Spatial Anchor UUID と anchor-relative pose を利用する場合、designation adapter と既存 `IFoodPlacementService` の関係を `Q5` で確定する。
- anchor が使用可能になるまで model を world origin など未検証位置へ表示しない。

### Selected Model Loader

- Menu selection の item ID を Catalog で解決し、その item の model data だけを cache または source から取得する。
- download、parse、instantiate の状態と cancellation/error/retry を公開する。
- model ready と anchor ready の両方が揃ったときだけ Food Instance を表示する。
- Network mode は order readiness、selected verified GLB metadata、downloaded byte SHA-256 の gate を通過してから glTF load する。

### Food Interaction Bounds

- model ready 後に mesh/renderer geometry から要求された「最遠2点基準」の透明 AABB collider を構成する。
- collider を食品 root の pose/scale に追従させる。
- bounds 抽出不能を明示的な non-interactive/error として扱う。
- 算出座標系と二点からの extent algorithm は `Q3` の決定対象とする。

### Scoop Detector and Feedback

- Spoon volume と Food AABB の接触を、debounce/exit/再進入などの規則で一回の scoop action に正規化する。
- 成立時に `FoodScooped`、visual/audio feedback、対応端末の任意 haptic を発行する。
- Haptic failure を game event の失敗にしない。

### Food Consumption State and Effects

- Food instance ごとに remaining portion を持ち、valid scoop ごとに単調減少させる。
- visual scale と collider scale/bounds を同期し、crumb effect を発生させる。
- 完食閾値で model/collider/effect を cleanup し、`DishCleared` を一度だけ発行する。
- 複雑な断面 mesh を生成しない。

### Session Reset Coordinator

- session token の cancel 後にも完遂できる reset command を提供する。
- Food instance、interaction state、crumb、selected item、menu/loading、Presenter、QR recognition session state を初期化する。
- Catalog/history/cache と運営者が確定した placement は、別の明示 policy なしに削除しない。

## Runtime Flows

### First-Time Guided Session

1. Attract で Start を受け、Session Orchestrator が Tutorial を開始する。
2. Tutorial が説明を提示し、Anchor Designation の成立 event を待つ。
3. ServeAppetizer command でリンゴ等を anchor へ表示する。
4. Scoop Detector が FoodScooped を発行し、Consumption State が縮小/crumb を更新する。
5. 完食時に DishCleared を一度発行し、Tutorial を完了する。
6. Scene を変えず FreePlay へ移り、Virtual Menu を表示する。
7. Menu selection → Catalog lookup → selected model load → anchor へ表示、の順に進む。
8. 完食後 Outro を経て Attract へ戻る。

### Menu and Model Loading

1. Contract Guard が接続先の v2 compatibility を確認する。
2. v2 Client が history/order state を Network catalog item へ変換し、Standalone Adapter が端末内 catalog を local item へ変換する。
3. Catalog は source namespace を保持したまま両 item set を集約する。一方の source failure で他方を捨てない。
4. Virtual Menu は一つの UI で両 source の metadata/preview/placeholder を一覧表示する。
5. Network preview は Preview Repository が selected/visible artifact を取得し、SHA-256 検証後に image cache へ publish する。Standalone preview は local reference/placeholder を用いる。
6. 選択された item の source に応じて、Network item は Selected Model Loader、Standalone item は local loader へ dispatch する。
7. Network loader は selected verified GLB revision の cache を確認し、必要な場合だけ Artifact Adapter から download/verify して parse する。Standalone loader は API request を行わず local model を parse する。
8. Anchor ready と model ready の両方が揃ったときに Food Instance を作る。
9. Source-specific error 時は他 source の選択を維持しつつ、item/context を保った retry または menu return を提示する。

### API State Refresh

1. History query で stable order/item identity と pagination cursor を取得する。
2. Processing/review item は order detail/status から order state と全 stage state を更新する。
3. Response version/revision が古い場合は newer local state を巻き戻さない。
4. `COMPLETED` かつ selected verified GLB が利用可能な item だけを model-ready candidate にする。GLB 早期公開は `Q11` 解決後に変更できる。
5. API/session cancel 後の late response は破棄する。

### Eating

1. Food Instance 作成後に Bounds が透明 AABB collider を生成する。
2. Spoon の有効な scoop を detector が一回に正規化する。
3. Feedback を再生し、任意の haptic と FoodScooped event を発行する。
4. remaining portion を減らし、visual/collider の縮小と crumb effect を同期する。
5. 完食閾値で cleanup 後に DishCleared を一度発行する。

### Abort

1. UserAbsent、idle timeout、staff reset、fatal rescue のいずれかが session cancel を要求する。
2. 実行中の step/load/interaction await が cancellation で終了する。
3. reset coordinator が cancel 済み token と独立して一時状態を cleanup する。
4. 3秒以内に Attract を表示し、履歴と有効な展示 placement は維持する。

## Failure Handling

| Failure | Required behavior |
|---|---|
| Preview image 取得失敗 | Placeholder、item 単位 retry。他項目を表示する |
| Model reference/取得/parse 失敗 | 食品を表示せず error/retry/menu return。別 item を選択可能にする |
| Anchor 未指定/復元失敗 | world origin へ出さず designation/reconfigure を案内する |
| Bounds 抽出不能 | Interaction ready にせず診断可能にする。見た目だけを成功扱いにしない |
| Haptic 非対応/失敗 | Scoop action と visual/audio feedback を継続する |
| Effect 生成失敗 | Consumption/DishCleared を停止させず warning を記録する |
| Viewer unreachable | VR session を停止させず、viewer 側で stale/offline/error を示す |
| Session abort during load/effect | 非同期処理を cancel し、遅延 callback が次 session に反映されないようにする |
| v1/unknown API contract | v2 として解析せず compatibility error。旧 route へ fallback しない |
| Unknown order/stage/artifact enum | Ready/Completed に推測せず item を non-selectable にして contract mismatch を記録する |
| Artifact SHA mismatch/truncation | Decode/load/cache publish せず隔離し、policy に従って再取得する |
| 401/403 | Credential/scope error として扱い、secret を log せず sign-in/reconfigure を案内する |
| 429/5xx/network timeout | Published retry guidance に従う。未定義の場合は bounded backoff と manual retry、session cancel を優先する |
| API unavailable/auth/contract mismatch | Network section/item を offline/error とし、Standalone item の一覧・選択・load を継続する |
| Standalone file missing/corrupt | 該当 local item だけ unavailable/error。他 local/network item と menu 全体は継続する |
| Network/Standalone identity collision | Source namespace を含む key で別 item として保持し、上書きしない |

## Verification Boundaries

- EditMode: step/condition、catalog item validation、selection/QR responsibility separation、portion state、event one-shot、reset policy。
- PlayMode: Tutorial→FreePlay、menu image-only loading、selected model gate、AABB/collider tracking、scoop debounce、shrink/crumb/disappear、abort cleanup。
- PlayMode/Offline: Tutorial 完了後の unified menu、Network/Standalone 同時表示、source dispatch、API failure 中の local selection/load、local file error isolation。
- Contract test: reviewed v2 schema fixture、全 enum、ProblemDetails、unknown enum、immutable artifact selection、SHA-256、v1 rejection。
- API integration: normative v2 OpenAPI に path/security/response が追加された後、mock/server と history/status/metadata/download/auth/compatibility contract test。
- Quest 3: Start/controller/QR designation、Spatial Anchor、spoon interaction、haptic、performance、連続 session。
- iPad 等: catalog/preview/status consistency、対象表示形式、同期、offline/error。対象方式は `Q2` 解決後に確定する。
