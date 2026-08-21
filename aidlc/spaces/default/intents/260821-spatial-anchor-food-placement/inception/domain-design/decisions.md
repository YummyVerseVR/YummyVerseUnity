# Architecture Decisions

## ADR-001: 食べ物の identity と placement を分離する

### Context

現行 QR flow は QR payload の GUID と QR Transform の双方を一つの検出サービスから配布し、食べ物取得と表示位置の両方を制御している。新要件では QR を表示位置基準から外す一方、通常モードの食べ物 GUID 入力は維持する。

### Decision

QR/Standalone selection を Food identity source、Spatial Anchor placement を Food pose source として別々に扱う。`FoodContext` は identity のみを処理し、`FoodViewModel` と `FoodView` は QR Transform を購読・追従せず Placement Model の ready pose を使用する。

### Consequences

- Positive: QR の移動、追跡揺れ、ロストが食べ物の表示位置へ影響しない。
- Positive: 通常モードと Standalone Mode が同じ配置設定を利用できる。
- Negative: 食べ物データと配置データの準備完了タイミングを調停する必要がある。
- Follow-up: QR Transform 関連 interface を削除するか、他用途のために残すかを実装参照調査後に決める。

### Alternatives Rejected

- QR Transform を Anchor 作成後も補正入力として残す:
  - QR ロストと追跡揺れへの依存が残り、要求された基準変更を満たさない。
- QR flow 全体を削除する:
  - 食べ物 GUID の入力源まで失い、要求範囲を越えて server flow を変更する。

### Traceability

- Requirements: `FR4`, `FR5`, `NFR5`

## ADR-002: Anchor UUID と Cube の anchor-relative pose を永続化する

### Context

Spatial Anchor の world pose は runtime の座標系だけでは次回起動時に再現できない。食べ物位置は Anchor 自体と一致するとは限らず、運営者が Cube で調整した offset/rotation が必要である。

### Decision

Meta Spatial Anchor を端末へ保存し、その UUID と Cube の local position/local rotation を schema version 付きの一レコードとして保存する。起動時は UUID で Anchor を load/localize した後に relative pose を適用する。Anchor 保存成功前に UUID を commit しない。

### Consequences

- Positive: 端末再起動後も同じ物理空間の展示位置を復元できる。
- Positive: Anchor と food placement の距離・向きを独立に調整できる。
- Negative: SDK の非同期 load/localization failure と保存データ migration を扱う必要がある。
- Follow-up: schema version を初回から保持し、形式変更時に migration または明示的な再設定を行う。

### Alternatives Rejected

- world position/rotation だけを PlayerPrefs へ保存する:
  - tracking origin が変わる再起動後の物理位置を保証できない。
- Anchor UUID だけを保存して food を Anchor 原点へ置く:
  - Cube で任意位置を設定する要求を満たさない。
- Cube を別 Spatial Anchor として保存する:
  - 単一配置に二つの Anchor lifecycle が必要となり、整合性と運用が複雑になる。

### Traceability

- Requirements: `FR2`, `FR3`, `FR6`, `FR7`, `FR8`, `NFR2`, `NFR3`

## ADR-003: 設定用 Cube は明示的な Editing 状態だけ grab 可能にする

### Context

常時 grab 可能な配置オブジェクトは来場者操作で誤って移動される可能性がある。運営者には controller で直感的に調整する手段と、確定・再編集の境界が必要である。

### Decision

設定画面を開いて `Editing` となった場合だけ Cube を表示し、controller grab interaction を有効化する。Anchor 未設定時は `Set / Update Spatial Anchor` 実行時の Cube world pose に Anchor を作成し、以後 Anchor 自体は grab 対象にしない。Cube は Anchor と別の world-space GameObject として動かし、`Lock Food Position` で relative pose を計算・保存して `Ready` に遷移する。再編集は設定画面を再度開く操作で開始する。

### Consequences

- Positive: 誤操作による食べ物位置の変更を防げる。
- Positive: Cube の見た目を preview としてそのまま利用できる。
- Positive: Anchor 基準の設定と食べ物位置の設定を同じ操作オブジェクトで二段階に行える。
- Negative: UI 状態と Cube の表示/interactable 状態を常に同期させる必要がある。
- Follow-up: 現地運用で Cube の大きさ、色、grab affordance を確認する。

### Alternatives Rejected

- Cube を常時表示・grab 可能にする:
  - 配置確定後も来場者が変更でき、固定要件を満たさない。
- UI の数値入力だけで位置を設定する:
  - controller で Cube を掴んで調整する要求を満たさない。

### Traceability

- Requirements: `FR1`, `FR3`, `FR4`, `FR8`, `FR10`, `NFR4`

## ADR-004: 復元失敗時は未検証の world pose を使わない

### Context

保存済み UUID が見つからない、localization できない、または永続レコードが破損している場合、前回の world pose は現在の tracking origin に対して正しい保証がない。

### Decision

Anchor と保存レコードを両方検証できるまで Placement Model は `Ready` を公開しない。失敗時は `Failed`/要再設定として設定画面へ通知し、食べ物を古い world pose に表示しない。通常の game/session reset は有効な配置設定を削除しない。

### Consequences

- Positive: 食べ物が意図しない物理位置へ表示される事故を防げる。
- Positive: 展示キャリブレーションと来場者セッションを独立して管理できる。
- Negative: Anchor が一時的に localize できない間、食べ物を表示できない。
- Follow-up: 現場 Runbook に scan/retry/reconfigure の順序を記載する。

### Alternatives Rejected

- 前回 world pose へ fallback する:
  - tracking origin の差を無視し、誤配置を正常に見せてしまう。
- session reset ごとに Anchor を削除する:
  - 展示運営者が来場者ごとに再設定する必要が生じる。

### Traceability

- Requirements: `FR7`, `FR9`, `FR10`, `NFR1`, `NFR2`

## Review

- Status: `READY`
- Approval basis: 2026-08-21 の利用者要求および要件 `[D1]`。
