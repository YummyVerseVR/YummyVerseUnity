# Requirements

## Intent Analysis

目的は、YummyVerseUnity の既存製品要件を変えずに、core logic、class dependency、View、DI、lifecycle、unused code の設計を再構成し、今後の変更が同じ混乱を再生産しない仕組みを作ることである。runtime root は active build scene とその再帰 graph、DI activation、Unity callback、Editor tests とする。

以下の ID はこの intent の恒久 ID であり、Construction/Verification で変更・再採番しない。要求の実装状況は、証拠が追加されるまで `PENDING` とする。

## Functional Requirements

### FR-AR-001: Active root と到達性を定義する (`MUST`)

`Assets/YummyVerse/Scene/Restaurant.unity`、その再帰 Prefab/asset graph、`NonLazy`/`IInitializable`、Unity lifecycle callback、Editor tests を含む active root を再現可能な手順で定義すること。

検証条件:

- `EditorBuildSettings` から enabled Scene が列挙され、唯一の active Scene が記録されている。
- Scene/Prefab attachment だけでなく、code call、DI activation、callback、serialized UnityEvent、ScriptableObject reference の経路が分類されている。
- Editor tests/editor tooling は runtime root と別に記録されている。

初期状態: `INITIAL-FACTS-RECORDED`。初期監査は `audit/codex-redesign.md` を参照する。

### FR-AR-002: Domain/Application の core 境界を確立する (`MUST`)

Domain は contracts/value objects/invariants、Application は use cases、session/business decisions、state transitions を担い、両者は MonoBehaviour/View concrete/network/filesystem/PlayerPrefs/Meta XR/glTF/input concrete を知らないこと。

検証条件:

- Domain/Application の compile-time reference に上記 concrete が存在しない。
- use case を Unity GameObject なしで呼び出せる seam と EditMode unit test がある。
- state transition と policy が View callback から分離されている。

### FR-AR-003: Core の責務を use case/collaborator へ移す (`MUST`)

catalog、food selection、placement、food interaction、session/reset、Tutorial/settings の判断と状態遷移を plain C# の use case/collaborator に集約し、View は判断を再実装しないこと。

検証条件:

- feature ごとに command/input、use case、state/result、port の組が設計資料にある。
- View 内の長い switch、I/O、catalog policy、session decision の除去または移行理由が記録されている。
- 同じ policy を Editor test から直接検証できる。

### FR-AR-004: MonoBehaviour View を薄くする (`MUST`)

View は serialized references、Unity lifecycle、render/input event forwarding、`Update`/`LateUpdate` の tick forwarding のみを担うこと。UI generation、network/file I/O、catalog policy、session/business decision、state transition、subscription ownership は plain C# collaborator/use case に置くこと。

検証条件:

- `FoodSelectionMenuView`、`FoodPlacementCubeView`、`FoodView`、`ConfigUIView` を含む対象 View ごとに、残す forwarding/rendering と抽出した責務が表で確認できる。
- `partial MonoBehaviour` や helper MonoBehaviour へ同じ責務を移しただけではない。
- settings UI の controller interaction、VR 空間上の表示順、serialized reference を含む既存の挙動が回帰確認対象になっている。

### FR-AR-005: 外部境界ごとに role-specific port を置く (`MUST`)

Network catalog、Standalone catalog、artifact transfer、placement、Spatial Anchor、QR designation、controller input、model loading など、外部境界ごとに利用者の役割を表す port を定義し、local/remote を同じ曖昧な汎用 interface の多重 bind にしないこと。

検証条件:

- 各 port に consumer、入力/出力、failure、lifecycle、実装 adapter が記録されている。
- Network と Standalone の identity namespace、availability、failure が別々に表現されている。
- generic `IFetchable`/`IService` の多重 binding を新たに追加していない。

### FR-AR-006: DI を composition root に集約する (`MUST`)

Installer は composition root として feature registration に委譲し、具象 `new`/bind、scope、`NonLazy`/`IInitializable` の起動責任を root で決定すること。空 Installer と、Scene に置かれているだけで意味を持たない component を残さないこと。

検証条件:

- `RestaurantInstaller` と feature registration の責務が記録されている。
- `FoodInstaller` の扱いを、Prefab/Scene serialized reference とともに決定する。
- DI container の起動時に missing binding、意図しない多重 binding、不要な NonLazy activation がない。

### FR-AR-007: State は read-only、変更は command とする (`MUST`)

View へ公開する状態は原則 read-only stream/property とし、変更は command method/use case によって行うこと。subscription、cancellation、disposal の owner を session または GameObject lifetime と一致させること。

検証条件:

- View/ViewModel の state reader と command interface が区別されている。
- 各 subscription の開始、cancel、dispose owner が feature 単位で記録されている。
- `OnDisable`/`OnDestroy`、session reset、再入場で callback が漏れない。

### FR-AR-008: Transport DTO を mapper 境界に閉じ込める (`MUST`)

HTTP/raw JSON/SDK/file DTO は Infrastructure adapter 内に限定し、mapper を通して Domain/Application 型へ変換すること。YummyService v2、Standalone、Meta XR、glTF の型を core/public View contract へ漏らさないこと。

検証条件:

- adapter ごとに DTO→application/domain mapping が明記されている。
- unknown enum、missing field、invalid artifact、SHA mismatch、timeout、cancel の mapping が source-specific result になる。
- v1/legacy route を v2 の代替として追加していない。

### FR-AR-009: 未使用コードを証拠付きで判定・削除する (`MUST`)

active runtime roots から code call、DI activation、Unity callback、serialized UnityEvent、ScriptableObject data reference のいずれでも到達しないものを未使用と定義すること。Scene/Prefab attachment だけを使用根拠にしないこと。

検証条件:

- 削除候補ごとに class reference、script GUID、active asset graph、tests/editor tooling の調査結果がある。
- runtime 非到達でも Editor test/editor tooling として必要なものはその理由が記録される。
- 削除後の再走査で missing script、未解決 GUID、DI failure、意図しない compile error がない。

### FR-AR-010: Serialized asset 変更を安全に行う (`MUST`)

script、Prefab、Scene、ScriptableObject の rename/move/delete は `.meta` GUID と参照を一組で検証し、Unity Editor load を完了条件に含めること。

検証条件:

- 変更前後の参照者と GUID が記録されている。
- `Missing (Mono Script)`、missing serialized field、UnityEvent、DI binding を検出する検証がある。
- Unity load 未実行の場合、コード compile 成功だけで asset 変更成功と判定しない。

### FR-AR-011: 既存の製品要件と体験を保持する (`MUST`)

再設計は責務と依存方向を変更しても、Spatial Anchor/anchor-relative pose、QR の anchor designation 専用化、YummyService v2、Standalone、Tutorial、catalog/preview/model、food interaction、settings UI の既存決定を変更しないこと。

検証条件:

- 変更対象 feature と保持する製品要件の traceability がある。
- 設定 UI の VR 表示順、controller interaction、session reset、Network/Standalone の独立性を回帰観点に含める。
- 未確認の Quest/PCVR 挙動は `NOT-RUN`/`UNKNOWN` と記録され、成功扱いされない。

## Non-Functional Requirements

### NFR-AR-001: 依存方向を architecture gate で enforce する (`MUST`)

新しい依存を View→application port、Application→Domain/port、Infrastructure→port implementation、composition root→concrete に限定し、逆依存を review で拒否できること。

検証方法: namespace/reference scan、DI registration review、domain/application の Unity concrete scan、architecture checklist。

### NFR-AR-002: Core を EditMode で高速・決定的に検証できる (`MUST`)

Domain value object と Application use case は Unity scene/device なしに EditMode unit test で検証可能であること。

検証方法: pure core の state transition、policy、cancel/reset、source selection の unit test。未追加の場合は理由と代替確認を test result に記録する。

### NFR-AR-003: Async/reactive lifecycle が漏れない (`MUST`)

UniTask/R3 の処理は呼出元の cancellation を受け、session/GameObject destruction/reset で安全に停止し、cancel を成功と誤認しないこと。

検証方法: normal completion、cancel、timeout、reset、destroy、再入場を含む lifecycle test。

### NFR-AR-004: Source-specific failure を隔離する (`MUST`)

Network と Standalone の identity、loading、failure、availability、lifecycle が分離され、一方の障害で他方が利用不能にならないこと。

検証方法: adapter contract test、network timeout/auth/contract mismatch、local missing/corrupt file、片系 offline の組合せ。

### NFR-AR-005: View の変更容易性を維持する (`MUST`)

View に business policy、UI construction、I/O、長い分岐を再導入せず、表示と forwarding の変更が use case の unit test と独立してレビューできること。

検証方法: View responsibility checklist、対象 View の collaborator boundary review、new `partial/helper MonoBehaviour` scan。

### NFR-AR-006: DI binding を決定的にする (`MUST`)

具象 bind と lifetime を composition root で一意に説明でき、曖昧な multi-bind、implicit global lookup、空 Installer がないこと。

検証方法: Installer graph review、container validation、NonLazy/IInitializable inventory、Scene/Prefab installer audit。

### NFR-AR-007: Serialized asset の参照完全性を守る (`MUST`)

rename/move/delete 後も `.meta` GUID、Prefab/Scene/ScriptableObject/UnityEvent、serialized field が完全であること。

検証方法: GUID/reference diff、Unity Editor load、missing script/field scan。未実行は `NOT-RUN`。

### NFR-AR-008: 設計判断と削除証拠を追跡できる (`MUST`)

requirements、components、ADR、delivery unit、code change、test result、unused evidence の対応を intent 内で追跡できること。FR/NFR ID は固定すること。

検証方法: `traceability.json` の全 ID 確認、delivery plan/verification の参照確認。

### NFR-AR-009: Platform 結果を混同しない (`MUST`)

Editor/Standalone、PCVR、Quest/Android/OpenXR、Unity load の結果を分離して記録し、未実行を成功扱いしないこと。

検証方法: platform 別 test matrix、device result と environment/version の記録。

### NFR-AR-010: 削除と移行に rollback がある (`MUST`)

unused code 削除、port migration、serialized asset change は、失敗時に限定的に戻せる単位と影響範囲を持つこと。既存製品要件を壊す rollback は採用しないこと。

検証方法: unit 単位の rollback plan、削除前後 graph、missing reference scan、変更差分 review。

### NFR-AR-011: 例外を期限付きで管理する (`MUST`)

architecture gate の例外には ID、理由、範囲、owner、期限、除去条件、代替検証、rollback を付け、intent/decision に記録すること。

検証方法: exception register review、期限切れチェック、除去条件の後続 intent traceability。

## Constraints and Baseline

- Unity Editor 基準は `ProjectSettings/ProjectVersion.txt` の `6000.2.6f2`。C#、UniTask、R3、Extenject の既存選択を尊重する。
- 初期 baseline `dotnet build Assembly-CSharp.csproj --no-restore --nologo` は 0 errors / 25 warnings と引き継いでいる。再設計後の結果ではない。
- documentation agent は `aidlc/` 以外を編集しない。コード実装、Prefab/Scene wiring、実機検証は実装 agent と後続 verification の責務である。
- `intents.json` は既存にないため、この intent では新規作成しない。

## Status

全 FR/NFR は設計・実装・検証の gate として固定した。初期監査と plan は記録済みだが、実装完了・削除完了・テスト成功は未確認である。
