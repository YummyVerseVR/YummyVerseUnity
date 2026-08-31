# Architecture Decisions

## ADR-AR-001: Active runtime root を削除判定の基準にする

### Context

Build settings には複数 Scene と legacy/sample code が存在し得る。Scene/Prefab attachment だけでは、実際の呼出し、DI activation、callback、serialized data の利用を区別できない。

### Decision

`EditorBuildSettings` で唯一 enabled の `Restaurant.unity`、再帰 Prefab/asset graph、`NonLazy`/`IInitializable`、Unity lifecycle callback、Editor tests を root とする。未使用判定は code call、DI activation、callback、serialized UnityEvent、ScriptableObject reference の到達性で行い、証拠を audit に残す。

### Consequences

- active runtime と legacy/sample/editor tooling を分けて監査できる。
- Scene に付いているだけの component を自動的に使用中とはみなさない。
- 削除前後の graph scan が必要になる。

### Traceability

`FR-AR-001`, `FR-AR-009`, `NFR-AR-008`, `NFR-AR-010`

## ADR-AR-002: Domain/Application を Unity 非依存の core とする

### Context

Model と View の具体型が use case へ漏れると、状態遷移・policy・I/O が Unity lifecycle に結び付き、unit test と再利用が難しくなる。

### Decision

Domain は contracts/value objects/invariants、Application は use cases/state transitions/business decisions とし、MonoBehaviour/View concrete/network/filesystem/PlayerPrefs/Meta XR/glTF/input concrete を参照しない。外部機能は port から利用する。

### Consequences

- core/use case を EditMode unit test で検証できる。
- 既存 Model の分類と段階的移行が必要になる。
- transport/SDK 型の mapper が必要になる。

### Traceability

`FR-AR-002`, `FR-AR-003`, `FR-AR-008`, `NFR-AR-001`, `NFR-AR-002`

## ADR-AR-003: 外部境界は role-specific port と source-specific adapter にする

### Context

Network と Standalone は identity、loading、failure、lifecycle が異なる。汎用 `IFetchable` を多重 bind すると解決順と暗黙 fallback に依存する。

### Decision

consumer ごとに port を定義し、Network catalog、Standalone catalog、artifact、placement、anchor、QR designation、input、model loading を別 adapter で実装する。Network と Standalone を一つの曖昧な generic interface に bind しない。

### Consequences

- source-specific error/availability を失わない。
- adapter 数と registration は増えるが、交換単位と contract test が明確になる。

### Traceability

`FR-AR-005`, `FR-AR-008`, `FR-AR-011`, `NFR-AR-004`, `NFR-AR-006`

## ADR-AR-004: MonoBehaviour は薄い境界 adapter とする

### Context

現行の大きな View には表示、UI 生成、I/O、policy、状態遷移、購読管理が混ざる。partial 化だけでは依存と責務が残る。

### Decision

View は serialized references、Unity lifecycle、render/input event forwarding、tick forwarding に限定する。UI generation、I/O、catalog/session decision、state transition、long switch、subscription ownership は plain C# collaborator/use case に抽出する。

### Consequences

- View の変更と core の unit test を分離できる。
- collaborator の DI、state rendering、Prefab serialized reference の移行が必要になる。
- controller interaction と VR display ordering の regression gate が必要になる。

### Traceability

`FR-AR-003`, `FR-AR-004`, `FR-AR-007`, `FR-AR-011`, `NFR-AR-005`

## ADR-AR-005: Installer を composition root として feature registration に委譲する

### Context

多くの binding と空の `FoodInstaller` が存在すると、どこで concrete が作られ、どの scope/activation で動くかが分からなくなる。

### Decision

root Installer は feature registration へ委譲し、concrete `new`/bind、scope、`NonLazy`/`IInitializable` を composition root に閉じ込める。空 Installer と implicit global lookup を禁止する。

### Consequences

- DI graph と lifecycle を一箇所でレビューできる。
- `FoodInstaller` の Prefab attachment と GUID 参照を確認した上で統合/削除する必要がある。

### Traceability

`FR-AR-006`, `NFR-AR-006`, `NFR-AR-010`

## ADR-AR-006: Read-only state、command、lifecycle owner を分離する

### Context

View が状態を直接変更したり、subscription の disposal を複数箇所が所有したりすると、session reset・destroy・再入場で二重実行と漏れが発生する。

### Decision

View には read-only stream/property を公開し、変更は command/use case method にする。各 UniTask/R3 subscription/request/effect の owner と cancel/dispose 条件を feature design に記録する。

### Consequences

- session/GameObject lifetime を境界にできる。
- state mapper と cancellation test を追加する必要がある。

### Traceability

`FR-AR-007`, `NFR-AR-002`, `NFR-AR-003`, `NFR-AR-004`

## ADR-AR-007: Transport DTO/SDK type は mapper 境界に閉じ込める

### Context

raw JSON、v2 DTO、Meta XR/GLTF 型を core や View が知ると、API contract と UI/業務ルールが密結合になる。v1/legacy を v2 fallback に戻してはならない。

### Decision

adapter 内で transport/SDK 型を受け、mapper が Domain/Application result へ変換する。unknown enum、required field 欠落、failure、cancellation、integrity mismatch は source-specific failure として fail closed にする。

### Consequences

- contract test と mapping test の境界が明確になる。
- v2 transport が未定義の部分を推測実装せず、NOT-READY として保持できる。

### Traceability

`FR-AR-005`, `FR-AR-008`, `NFR-AR-004`, `NFR-AR-008`

## ADR-AR-008: Serialized asset と削除は GUID/graph/Unity load gate を通す

### Context

script の移動・削除だけでなく `.meta` GUID、Prefab/Scene/ScriptableObject/UnityEvent の serialized reference が壊れる。compile 成功だけでは missing script を発見できない。

### Decision

rename/move/delete は `.meta` と参照者を同時に確認し、削除候補は class reference、script GUID、active asset graph、tests/editor tooling の証拠を残す。Unity Editor load、必要な Editor/PlayMode/device test を別 gate とする。

### Consequences

- 削除に時間はかかるが、無関係な legacy を保ったままにする/壊れた reference を残すリスクを減らす。
- 未実行の Unity/device test を成功と書けない。

### Traceability

`FR-AR-009`, `FR-AR-010`, `FR-AR-011`, `NFR-AR-007`, `NFR-AR-009`, `NFR-AR-010`

## ADR-AR-009: Architecture gate と期限付き例外で再発を防ぐ

### Context

短期の移行都合で reverse dependency、static lookup、空 Installer、View の I/O が再導入される可能性がある。無期限の例外は規約を無効にする。

### Decision

レビュー checklist を全変更へ適用し、例外は `EX-AR-###`、理由、範囲、owner、期限、除去条件、代替検証、rollback を intent/decision に記録する。期限切れ例外は新規変更を通さない。

### Consequences

- migration の現実的な段階性を保ちつつ、例外の恒久化を防げる。
- review と後続 intent の追跡が必要になる。

### Traceability

`FR-AR-002`, `FR-AR-004`, `FR-AR-006`, `FR-AR-009`, `NFR-AR-001`, `NFR-AR-005`, `NFR-AR-011`

## Exceptions

この設計時点で承認済みの architecture exception はない。実装中に必要になった場合は `EX-AR-###` を発行し、shared knowledge の手続きに従って期限付きで記録する。
