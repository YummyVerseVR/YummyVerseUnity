# Architecture Decisions

## ADR-001: Tutorial をデータ駆動の観測層として維持する

### Context

移管元仕様は展示現場での文言変更、離脱、停滞、シームレスな FreePlay 移行を重視している。Game の具象を Tutorial から直接駆動すると、step と game state が二重管理になる。

### Decision

Tutorial は `Present → Await → Succeed` の ScriptableObject step sequence とし、Narration/Task/Choice の3具象だけを使う。Game event を Condition から購読し、Game command と Presenter interface を境界にする。AppState は4状態だけを持ち、session cancellation と一括 reset で離脱を扱う。

### Consequences

- Positive: 文言、秒数、順序、救済を code change なしで調整できる。
- Positive: Tutorial と FreePlay が同じ game feature/event を共有できる。
- Negative: asset/DI/event wiring の整合性検証が必要になる。
- Follow-up: 新しい AnchorDesignated event と既存 QrPlateDetected asset の migration を計画する。

### Traceability

- Requirements: `FR1`〜`FR11`, `FR23`, `FR24`, `NFR1`〜`NFR4`, `NFR7`, `NFR8`

## ADR-002: 食品 catalog で preview と model data を分離する

### Context

生成履歴を一覧するために全 3D model を読み込むと network、parse、GPU/memory 負荷が増える。一方、選択後には既存 model data を再利用して速やかに表示したい。

### Decision

Catalog item は metadata、preview image reference、model data reference を分離して保持する。Virtual/Physical Menu は metadata と image だけを先行取得し、3D payload は選択 item だけを cache または source から load する。

### Consequences

- Positive: 一覧表示の負荷を画像中心に抑えられる。
- Positive: VR と外部 viewer が同じ item identity を共有できる。
- Negative: preview と model の整合性、cache lifecycle、selection latency の設計が必要になる。
- Follow-up: `Q1` の latency/cache policy と `Q2` の viewer contract を決定する。

### Traceability

- Requirements: `FR12`〜`FR15`, `FR24`, `NFR5`〜`NFR8`

## ADR-003: QR を anchor designation、メニューを food identity source とする

### Context

旧 intent は QR payload/GUID を food identity に残していた。2026-08-24 の要求は、QR 読込による model 生成・表示待ちを廃止し、QR を出現場所の指定だけへ限定している。

### Decision

Food identity は Generated Food Catalog/Virtual Menu の item ID から得る。QR recognition は Anchor Designation boundary にだけ入力し、model generation、item selection、download key へ接続しない。選択 model は designation 済み placement へ表示する。

### Consequences

- Positive: 同じ anchor で生成履歴を切り替えられ、QR 読込を model selection の待ち時間から外せる。
- Positive: identity、preview/model delivery、placement の責務が明確になる。
- Negative: 現行 QR GUID flow と tutorial asset/event を migration する必要がある。
- Supersedes: `260821-spatial-anchor-food-placement` の QR GUID 継続に関する `FR5`、`NFR5`、`ADR-001`。
- Follow-up: `Q5` を解決し、既存 persistent Spatial Anchor flow と QR designation adapter の関係を確定する。

### Traceability

- Requirements: `FR13`, `FR16`, `FR17`, `FR24`, `NFR6`〜`NFR8`

## ADR-004: 食事表現は AABB、段階縮小、軽量 effect で近似する

### Context

生成 mesh は形状が一定でなく、正確な切断面生成は複雑で高負荷になる。要求されているのはカジュアルに「すくった/食べた/なくなった」と理解できる表現である。

### Decision

model geometry から透明 AABB collider を作り、spoon scoop を一回の action に正規化する。action ごとに remaining portion を減らして visual/collider を縮小し、crumb effect を発生させ、完食時に cleanup と DishCleared を一度だけ行う。複雑な断面 mesh は生成しない。

### Consequences

- Positive: 不定形 model に共通の低コスト interaction を適用できる。
- Positive: Tutorial と FreePlay が同じ FoodScooped/DishCleared event を使える。
- Negative: AABB は形状に厳密ではなく、空間を含む false positive が起こり得る。
- Follow-up: `Q3` の最遠2点/AABB algorithm と、Quest 実機で許容できる interaction tolerance を決定する。

### Traceability

- Requirements: `FR18`〜`FR23`, `FR24`, `NFR4`, `NFR6`〜`NFR9`

## ADR-005: 永続資産と来場者 session state を別 lifecycle にする

### Context

展示では来場者が途中離脱し、次の来場者へ速やかに戻す必要がある。一方、生成履歴と運営者が確定した placement を毎回消すと運用が成立しない。

### Decision

Session reset は selected item、Food Instance/portion/collider/effect、UI/loading、TutorialContext、recognition session state だけを cleanup する。Catalog/history/cache と確定済み anchor/placement は別 policy とし、通常 reset で暗黙に削除しない。

### Consequences

- Positive: 2人目へ状態汚染を残さず、生成結果と会場 calibration を再利用できる。
- Negative: 明示的な削除、期限、cache eviction、stale viewer data の policy が別途必要になる。
- Follow-up: `Q1` と運営要件で history/cache retention を決定する。

### Traceability

- Requirements: `FR8`, `FR12`, `FR17`, `FR24`, `NFR3`, `NFR4`, `NFR7`, `NFR8`

## ADR-006: YummyService v2 を専用 client boundary で利用し、transport 未定義時は fail closed にする

### Context

利用 API version は v2 と指定された。YummyService の normative v2 OpenAPI/README は workflow/state/schema を定義する一方、OpenAPI `paths` は空、server URL は placeholder、authentication と artifact lookup/download は deferred である。Current Unity client は QR GUID から旧 `/{guid}/model` を呼び、v2 order/artifact vocabulary と互換性がない。

### Decision

YummyService v2 の HTTP/DTO は専用 `YummyService v2 Client` と Contract Guard に隔離する。New Network mode は v2 compatibility を確認し、v1 route/response や旧 GUID endpoint へ fallback しない。v2 domain mapping/fixture は先行できるが、production transport adapter は normative OpenAPI に required capabilities の paths/security/responses が追加されるまで `NOT-READY` とする。

### Consequences

- Positive: v2 と v1/legacy の identity/state/transport が混在しない。
- Positive: Draft contract の変更を一つの adapter と compatibility test に閉じ込められる。
- Negative: YummyService v2 の transport contract 公開までは end-to-end Network mode を実装完了できない。
- Follow-up: `API-CAP-01`〜`API-CAP-09` を YummyService 側の normative OpenAPI と mock/server へ追加し、`Q6`〜`Q10` を解決する。

### Alternatives Rejected

- v1 order/artifact endpoints を名前だけ v2 として流用する:
  - v2 の workflow/state/completion と契約が異なり、利用者指定の version を満たさない。
- 旧 `/{guid}/model` を暫定 fallback にする:
  - QR/GUID identity を復活させ、`FR16` と v2 immutable artifact model に反する。
- Empty `paths` から endpoint を推測する:
  - Method、auth、status、visibility、download semantics を捏造することになる。

### Traceability

- Requirements: `FR25`〜`FR30`, `FR32`, `FR33`, `NFR10`〜`NFR13`

## ADR-007: Model/preview cache は selected immutable artifact revision と SHA-256 を基準にする

### Context

YummyService v2 の artifact revision は immutable で、artifact ID/type/revision/SHA-256/verified を持ち、current selection は別 pointer である。Current Unity downloader は response bytes を base64 往復し、全 request で固定名 `test.glb` を使うため、revision identity、integrity、並行 download safety を満たさない。

### Decision

Preview/GLB は selected `ArtifactRef` を解決した後、artifact ID + revision + SHA-256 を cache identity とする。一意 temp file へ streaming download/incremental SHA-256 を行い、一致した bytes だけを atomic に cache publish/decode/load する。Preview と model の download/cache queue は分離する。

### Consequences

- Positive: Stale/mismatched/partial artifact を表示・実行しない。
- Positive: Immutable revision の再利用、並行 request、cross-device identity を正しく扱える。
- Positive: Base64 往復による余分な memory copy を除去できる。
- Negative: Artifact metadata lookup、file lifecycle、cache eviction、hash cost の実装が必要になる。
- Follow-up: `Q8`/`Q10` と cache/SLA の `Q1` を解決する。

### Alternatives Rejected

- Order ID または mutable filename だけで cache する:
  - Selected revision change と integrity identity を表現できない。
- Server の `verified=true` だけを信頼して client hash を省略する:
  - Transport/cache corruption を検出できない。
- Response 全体を memory 上で base64 encode/decode する:
  - 大きい GLB で peak memory とコピー回数が増える。

### Traceability

- Requirements: `FR29`〜`FR31`, `FR33`, `NFR11`, `NFR12`, `NFR14`

## ADR-008: Tutorial 後の一つの選択 UI へ Network と Standalone を独立 source として統合する

### Context

Game flow は Tutorial 後に食品メニューを表示する。利用者は YummyService v2 由来の生成食品に加え、端末へ保存した Standalone 食品も今後継続利用する。Standalone を Network fallback として実装すると、API error が local flow を停止させたり、廃止済み v1 policy と混同されたりする。

### Decision

Network Catalog Adapter と Standalone Catalog/Loader Adapter を独立 source とし、source namespace を保持した共通 `GeneratedFoodItem` model へ変換する。S14 後のFreePlayでは一つの Virtual Menu が両 source を同時表示し、selection source に応じて Network loader または local loader へ dispatch する。Network failure は Standalone の列挙・選択・表示を block しない。

### Consequences

- Positive: 来場者は Tutorial 後に一箇所から online generated food と端末内 food を選べる。
- Positive: v2 API が未準備/offline でも展示体験を Standalone で継続できる。
- Positive: Standalone は v1 fallback ではなく API 非依存の第一級 source として維持される。
- Negative: Identity namespace、source label、preview差、error state、selection dispatch を UI/domain で扱う必要がある。
- Follow-up: 同名 item の表示規則、source filter/sort、Standalone preview asset の生成/placeholder policy を UI design で確定する。

### Alternatives Rejected

- Network/Standalone で別々の post-tutorial menu を表示する:
  - 来場者が source mode を先に理解・切替する必要があり、一つの食品一覧という要求を満たさない。
- API failure 時だけ Standalone menu へ切り替える:
  - 平常時に local food を選べず、両方を同時表示する要求を満たさない。
- Standalone を v1 API fallback として残す:
  - Standalone は API request を行わない local feature であり、v1恒久廃止方針と責務が異なる。

### Traceability

- Requirements: `FR12`〜`FR14`, `FR24`, `FR34`, `FR35`, `NFR2`, `NFR3`, `NFR7`

## Review

- Status: `NOT-READY`
- Basis: 要件を支える主要な責務と判断は定義済み。`Q1`〜`Q11` の該当項目を Unit ごとに解決し、YummyService v2 の transport contract が公開されるまで Construction 全体の設計確定とは扱わない。
