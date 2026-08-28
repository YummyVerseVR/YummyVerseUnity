# Architecture and Code Quality

## 目的と適用範囲

この文書は、YummyVerseUnity のコアロジック、依存関係、View 層、DI、非同期処理、未使用コード削除を継続的にレビューするための共有規約である。製品機能の正規要件は各 intent に置き、この文書は intent をまたいで適用する設計・品質の不変条件を置く。

今回の再設計では、runtime root を `ProjectSettings/EditorBuildSettings.asset` で唯一 enabled の `Assets/YummyVerse/Scene/Restaurant.unity` とする。その Scene から再帰的に参照される Prefab/asset graph、Extenject の `NonLazy`/`IInitializable`、Unity lifecycle callback、Editor tests も root として扱う。別 Scene、Prefab、古い Script が存在することは、それだけでは active runtime の使用根拠にならない。

既存の Tutorial、Spatial Anchor、QR、Virtual/Physical Menu、Standalone、YummyService v2、食事 action の製品判断は変更しない。実装との不一致はこの規約を理由に黙って変更せず、対応する intent の gap として記録する。

## レイヤーと責務

### 依存の基本形

```text
          composition root
          /              \
     concrete bind       feature registration
          |                    |
Infrastructure adapter ---> application port
                                  ^
                                  |
Presentation/ViewModel ---> Application use case
                                  |
                                  v
                         Domain contracts/value objects
```

矢印は「その層が参照・実装する契約」を表す。新しいコードで許可される依存は次の四種類だけである。

| 依存元 | 依存先 | 許可される責務 | 具体例 |
| --- | --- | --- | --- |
| Domain | Domain contracts/value objects | 不変条件、値の検証、純粋な計算 | `FoodItemId`、portion の減少、状態遷移規則 |
| Application | Domain と role-specific port | use case、session policy、状態遷移、複数 port の調停 | `SelectFoodUseCase`、`ResetSessionUseCase` |
| Infrastructure | Application/Domain の port | HTTP、filesystem、PlayerPrefs、Meta XR、glTF、input、clock の adapter | `YummyServiceCatalogAdapter`、`SpatialAnchorGateway` |
| Presentation | Application port/use case と presentation state | ViewModel、Presenter、表示状態の変換、ユーザー操作の command 化 | `ConfigPresenter`、`FoodMenuViewModel` |
| Composition root | concrete と feature registration | bind、`new`、lifetime/scope の決定 | `RestaurantInstaller`、feature installer |

Domain と Application は `MonoBehaviour`、Unity View concrete、network/filesystem/PlayerPrefs、Meta XR、glTF、input の具体実装を知らない。Presentation は Application の role-specific port を利用し、Infrastructure はその port を実装する。Infrastructure から View concrete へ、View から Infrastructure concrete へ、Domain/Application から Unity concrete へ逆向きの参照を追加してはならない。

既存の `Model`/`ViewModel`/`View` というディレクトリ名は移行中の地図であり、正しい責務の証拠ではない。新しい責務を追加するときは名前ではなく上表の依存境界を優先する。既存型を段階的に移す場合は、移行元、移行先、暫定 adapter、削除条件を intent に記録する。

### Core / Domain

Domain は外部 I/O を持たない plain C# の最小単位である。

- immutable な value object、identity、domain result/error、状態遷移規則を持つ。
- `UnityEngine.Object`、`MonoBehaviour`、`ScriptableObject`、raw JSON、transport DTO、SDK type を公開契約に含めない。
- clock、random、ID 発行などが必要なら Domain 内で直接取得せず、Application または port から値を渡す。
- policy と invariant はここに置くが、画面表示、retry/backoff、ファイル名、HTTP status の判断は置かない。

### Application / Use case

Application は「何をするか」を表す。Domain を組み合わせ、外部境界へ role-specific port を呼び、利用者へ read-only な結果を公開する。

- session、catalog、selection、placement、eating、reset の状態遷移と業務判断を持つ。
- port の実装型、Unity lifecycle、raw transport DTO を参照しない。
- use case の入力は command/value object、出力は result/read-only state とする。
- UI のための表示文字列や `GameObject` 操作を持たない。必要なら Presentation が mapper する。

### Ports

port は「誰が何を必要としているか」を役割ごとに表す。大きな汎用 interface を作って複数 source の差異を隠さない。

| 境界 | 例となる port | 禁止する混同 |
| --- | --- | --- |
| Network catalog | `INetworkFoodCatalogReader` | local file 列挙を同じ `IFoodReader` に bind しない |
| Standalone catalog | `IStandaloneFoodCatalogReader` | HTTP timeout/auth failure と file missing を同じ error にしない |
| Artifact transfer | `ISelectedArtifactReader` | preview と GLB の policy を一つの downloader に詰めない |
| Placement | `IFoodPlacementStore`、`ISpatialAnchorGateway` | UUID persistence と QR designation を一つの mutable context にしない |
| Device input | `IControllerCommandSource`、`IQrDesignationSource` | controller と QR の identity を同じ input enum に潰さない |
| Model loading | `IFoodModelLoader` | bytes download、parse、instantiate の責務を View に置かない |

例えば Network と Standalone の両方を次のように曖昧に bind してはならない。

```csharp
// 悪い例: source-specific failure と identity を失い、多重 bind の解決順に依存する。
public interface IFetchable<T> { UniTask<T> FetchAsync(string key, CancellationToken ct); }
Container.Bind<IFetchable<FoodItem>>().To<NetworkFoodFetcher>().AsSingle();
Container.Bind<IFetchable<FoodItem>>().To<LocalFoodFetcher>().AsSingle();
```

代わりに利用者の意図を契約へ出し、composition root で個別に bind する。

```csharp
public interface INetworkFoodCatalogReader { UniTask<NetworkCatalogResult> ReadAsync(CancellationToken ct); }
public interface IStandaloneFoodCatalogReader { UniTask<StandaloneCatalogResult> ReadAsync(CancellationToken ct); }

Container.Bind<INetworkFoodCatalogReader>().To<YummyServiceCatalogAdapter>().AsSingle();
Container.Bind<IStandaloneFoodCatalogReader>().To<PersistentFoodCatalogAdapter>().AsSingle();
```

### Infrastructure / Adapters

Infrastructure は外部技術を知ってよいが、その知識を core へ漏らさない。

- transport DTO/raw JSON は adapter 内だけで使い、mapper で Domain/Application 型へ変換する。
- Network は order/item/artifact identity、HTTP status、timeout、retry、stale response、契約不一致を Network-specific result へ変換する。
- Standalone は local namespace、file missing、corrupt bytes、loader failure を Standalone-specific result へ変換する。
- Meta XR/Spatial Anchor、PlayerPrefs、filesystem、glTF、input はそれぞれ adapter として隔離する。
- SDK が要求する `FindObject` 相当が避けられない場合でも、device adapter の境界内に閉じ込め、core・Presentation からは port 経由で扱う。

```text
transport DTO -> v2 mapper -> application catalog item
local file    -> local mapper -> application catalog item
                         \       /
                          source-aware use case
```

同じ `FoodItemId` に見えても、Network order/artifact identity と Standalone file identity が衝突しないよう、source namespace を value object に含める。Network の失敗で Standalone を unavailable にしたり、その逆を行ったりしない。

### Presentation と ViewModel

Presentation は Application の結果を表示モデルへ変換し、ユーザー操作を command に変換する。表示 policy は ViewModel/Presenter に置き、View の MonoBehaviour へ戻さない。

- ViewModel/Presenter は use case/port から read-only state を購読し、表示用の text、sprite、visibility、loading state を組み立てる。
- 変更は `OpenSettingsCommand`、`SelectFoodCommand`、`ConfirmPlacementCommand` のような明示的 method/command にする。
- subscription の開始、cancel、disposal owner を ViewModel/Presenter ごとに定義する。
- View concrete の型、`Transform`、`Canvas`、`TMP_*` は Presentation の表示 adapter 以外へ漏らさない。

## View 規約

MonoBehaviour は Unity の life-cycle と境界の adapter であり、feature の頭脳ではない。許可する責務は次の通りである。

1. Inspector の serialized reference を保持し、欠落を早期に検知する。
2. `Awake`/`OnEnable`/`Start`/`OnDisable`/`OnDestroy` で collaborator の attach/detach を行う。
3. Unity UI/renderer/audio/input callback を plain C# collaborator の method/command/event へ forwarding する。
4. `Update`/`LateUpdate` は必要な tick を一回転送する。時間、retry、状態遷移をそこで判断しない。
5. collaborator が出した read-only state を UI/renderer に反映する。

次の責務は View に置かない。

- UI tree、ボタン、menu item の動的生成
- HTTP、filesystem、PlayerPrefs、download、decode、glTF import
- catalog の選択 policy、preview/model load policy、Network/Standalone の fallback
- session reset、business decision、状態遷移、長い `switch`
- subscription の共有、retry、cancellation、disposal の所有判断
- `partial MonoBehaviour` や別の helper MonoBehaviour への責務逃がし

```csharp
// 悪い例: 表示 callback が network、policy、UI生成、状態遷移を所有する。
public sealed class FoodMenuView : MonoBehaviour
{
    public async void OnOpenClicked()
    {
        var json = await UnityWebRequest.Get(endpoint + "/foods").SendWebRequest();
        foreach (var item in Parse(json)) CreateButton(item);
        if (controllerMode) ShowOnlyLocalItems();
    }
}
```

```csharp
// 良い例: View は event forwarding と state rendering だけを持つ。
public sealed class FoodMenuView : MonoBehaviour
{
    [SerializeField] private Button openButton;
    [SerializeField] private FoodMenuRenderer renderer;
    private IFoodMenuPresenter presenter;

    public void Initialize(IFoodMenuPresenter value) => presenter = value;
    private void OnEnable() => openButton.onClick.AddListener(ForwardOpen);
    private void OnDisable() => openButton.onClick.RemoveListener(ForwardOpen);
    private void ForwardOpen() => presenter.OpenMenu();
    public void Render(FoodMenuState state) => renderer.Render(state);
}
```

`FoodSelectionMenuView`、`FoodPlacementCubeView`、`FoodView`、`ConfigUIView` のように大きくなった View は、単に partial 化せず、表示状態、input forwarding、UI construction、use case orchestration のどれが混ざっているかを分けて抽出する。既存の VR sorting/overlay、controller interaction、serialized reference の挙動は View の薄型化後も回帰テストで保持する。

## DI と Composition Root

Installer は依存解決の composition root であり、feature registration の入口である。

- `RestaurantInstaller` のような root installer は feature ごとの registration method/installer へ委譲する。
- port と concrete adapter の bind、scope、`NonLazy`、`IInitializable` の起動責任を一箇所で読めるようにする。
- concrete の `new` は composition root に限定し、use case、ViewModel、View から concrete を new しない。
- `NonLazy` は session root/常駐 lifecycle が必要な component のみに使い、理由を registration に残す。
- 空の Installer、Scene/Prefab に置かれているだけで `InstallBindings` が空の component、暗黙の side effect を持つ installer を残さない。
- `FindObjectOfType`、service locator、static singleton は通常の解決経路に使わない。避けられない SDK access の adapter 内に限り、例外記録を伴って許可する。

```csharp
public override void InstallBindings()
{
    InstallFoodFeature();
    InstallPlacementFeature();
    InstallPresentationFeature();
}

private void InstallFoodFeature()
{
    Container.Bind<IFoodCatalog>().To<FoodCatalogUseCase>().AsSingle();
    Container.Bind<INetworkFoodCatalogReader>().To<YummyServiceCatalogAdapter>().AsSingle();
    Container.Bind<IStandaloneFoodCatalogReader>().To<PersistentFoodCatalogAdapter>().AsSingle();
}
```

Feature registration のどこで `IInitializable`/`NonLazy` が activate されるか、また Scene 上の serialized component がどの port に接続されるかを設計資料に記す。多重実装は意図的な collection bind と source selector がある場合だけ許可する。

## State、async、reactive の lifecycle

### Read-only state と command

View へ公開する状態は原則 read-only property/stream とする。状態を直接 set させず、変更は use case の command method へ集める。

```csharp
public interface ISettingsStateReader
{
    ReadOnlyReactiveProperty<SettingsState> State { get; }
}

public interface ISettingsCommands
{
    UniTask ConfirmAsync(SettingsDraft draft, CancellationToken ct);
}
```

### 所有者表

| 資源 | owner | 開始 | cancel/dispose |
| --- | --- | --- | --- |
| 来場者 session の UniTask | Session/Application orchestrator | session start | reset、user absent、fatal rescue |
| View の state subscription | ViewModel/Presenter または View scope | `OnEnable`/DI initialize | `OnDisable`/scope dispose |
| Scene 常駐 subscription | feature composition root | `IInitializable.Initialize` | scope dispose/application shutdown |
| request/stream | 呼出元 use case | command 実行時 | caller token、timeout、source disposal |
| temporary GameObject effect | effect/use-case coordinator | domain event 到着時 | session reset、completion、destroy |

UniTask は呼出元の `CancellationToken` を下位 adapter へ渡す。R3 の subscription は session token または GameObject lifetime と結合し、`OnDestroy` 後に callback が View を触らないようにする。cancel を成功・完了として扱わず、reset の cleanup は reset 自身の token に巻き込まれないようにする。

Network request の cancellation と Standalone file read の cancellation は同じ `CancellationToken` を受けられても、failure identity、retry、availability は source ごとに保つ。stale response は use case が request generation/version を検査して無視する。

## 未使用コードの判定と削除手順

### 定義

未使用コードとは、active runtime roots から次のいずれでも到達しない class、method、asset、binding、test helper である。

- code call/reference
- DI activation（`NonLazy`、`IInitializable`、factory、installer bind）
- Unity lifecycle callback（`Awake`、`Start`、`Update`、`OnEnable` など）
- serialized UnityEvent/reference
- ScriptableObject data reference

Scene/Prefab に component が付いているだけ、古い `*.meta` があるだけ、namespace/type が存在するだけでは使用根拠としない。Editor test/editor tooling は別の root として確認し、runtime から未到達でもテスト資産として必要なら「使用中」と記録する。

### 証拠を残す手順

1. `EditorBuildSettings` から enabled Scene を列挙し、今回の root を固定する。
2. Scene → Prefab/asset → ScriptableObject/UnityEvent → component の再帰 graph を追う。
3. root から code reference、DI bind/activation、Unity callback、serialized UnityEvent、ScriptableObject reference を走査する。
4. class reference、script GUID、asset GUID、active graph、tests/editor tooling を削除候補ごとに表へ記録する。
5. candidate の削除または detach を行う場合は、対応 `.meta`、Prefab/Scene reference、asmdef/namespace、Editor test を確認する。
6. C# compile、EditMode test、Unity Scene/Prefab load を実行し、結果を `PASS`/`FAIL`/`NOT-RUN` で記録する。
7. 削除後に同じ root scan を再実行し、残存 reference、missing script、未解決 GUID、DI activation failure がないことを確認する。

「Scene で使われているが、実際に呼ばれていない」場合は、serialized attachment を根拠に残さず、上記の到達性を調べる。到達経路がないものは削除候補として intent に証拠を残し、削除の影響を確認する。

## Serialized asset と GUID の安全規約

`.cs` の rename/move/delete と `.prefab`/`.unity`/`.asset`/`.meta` の変更を独立したテキスト編集として扱わない。

- source と `.meta` を常に一組で扱い、GUID を不用意に再生成しない。
- Scene/Prefab/ScriptableObject/UnityEvent の参照者を GUID と type の両方で確認する。
- `Missing (Mono Script)`、missing serialized field、DI binding error を Unity Editor load で検出する。
- asset graph 変更を含む削除は、削除前後の参照一覧と rollback 方法を intent/audit に記録する。
- Unity Editor の load 未実行の場合、設計・コードの検査が成功しても asset change の成功とはしない。

## Architecture gate と code review checklist

各変更はレビュー時に次を確認する。該当なしの場合も `N/A` と理由を書く。

- [ ] active runtime root、Editor test root、対象 asset graph が明記されている。
- [ ] Domain/Application が Unity/View/network/filesystem/PlayerPrefs/Meta XR/glTF/input concrete を参照していない。
- [ ] 新しい依存が View → application port、Application → Domain/port、Infrastructure → port implementation、composition root → concrete の許可方向だけである。
- [ ] port が利用者の役割固有で、local/remote を曖昧な generic interface にまとめていない。
- [ ] transport DTO/raw JSON/SDK type が mapper 境界を越えていない。
- [ ] View MonoBehaviour が serialized refs、lifecycle、render/input forwarding、tick forwarding に留まっている。
- [ ] UI 生成、I/O、catalog/session policy、state transition、long switch、subscription ownership が plain C# collaborator/use case にある。
- [ ] partial/helper MonoBehaviour で規約を迂回していない。
- [ ] Installer は feature registration へ委譲し、空 Installer がなく、NonLazy/IInitializable の理由が読める。
- [ ] state は read-only、変更は command、subscription/cancellation/disposal の owner が明記されている。
- [ ] Network/Standalone の identity、failure、lifecycle、availability が分離されている。
- [ ] 削除候補に class ref、script GUID、active asset graph、tests/editor tooling の証拠がある。
- [ ] EditMode unit、adapter contract、Unity load、Quest、PCVR/Editor のテスト結果が分離され、未実行は成功扱いされていない。
- [ ] serialized asset の rename/move/delete に `.meta` GUID と参照検証がある。
- [ ] 既存製品要件（Spatial Anchor、QR の designation 専用、Standalone、v2、設定 UI など）に対する回帰が確認または未確認として記録されている。

## 例外手続き

規約の例外は、実装を通すための無期限な注記ではない。次の項目を intent の `decisions.md` または audit に記録し、review で承認する。

| 項目 | 必須内容 |
| --- | --- |
| Exception ID | `EX-AR-###` の一意 ID |
| 理由 | SDK 制約、移行順序、性能・互換性上の具体的理由 |
| 範囲 | 例外となる file/class/port と許可する依存 |
| owner | 除去まで責任を持つ担当/agent |
| 期限 | 日付または release/intent boundary |
| 除去条件 | 何が整えば例外を消せるか |
| 代替検証 | 例外期間に必要な unit/contract/Unity/device test |
| rollback | 例外を戻す手順と、失敗時の安全な挙動 |

`FindObject`、service locator、static singleton、空 Installer、View の I/O などを例外にする場合は、adapter boundary へ閉じ込める設計と除去 task を同時に登録する。期限切れ、owner 不在、除去条件未達の例外は新しい依存の追加を許可しない。

## この intent への適用

`aidlc/spaces/default/intents/260828-architecture-redesign/` は、上記規約を YummyVerseUnity の初期監査へ適用する記録である。`audit/codex-redesign.md` は初期事実と削除証拠の形式を定義し、`inception/` は再設計、`construction/implementation.md` は実装予定を記録する。実装中のコード変更、build、Unity/Quest/PCVR 結果は、確認できるまでこの文書や intent で完了扱いにしない。
