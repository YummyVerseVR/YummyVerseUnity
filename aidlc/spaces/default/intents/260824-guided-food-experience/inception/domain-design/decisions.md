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

## Review

- Status: `NOT-READY`
- Basis: 要件を支える主要な責務と判断は定義済み。`Q1`〜`Q5` の該当項目を Unit ごとに解決するまで Construction 全体の設計確定とは扱わない。
