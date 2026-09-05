# Architecture Decisions

## ADR-CC-001: フェーズ順序・再送・タイムアウトは Model 層 (`ChewingSensorService`) に置く

### Context

キャリブレーションのフェーズ順序 (`CAL_NOISE` → `CAL_CHEW`)、再送、タイムアウトの判断をどこに置くかで、View/ViewModel が通信プロトコルの詳細を知ってしまうか、Model がプロトコルに閉じるかが決まる。

### Decision

`ChewingSensorService` (Model) がフェーズ順序、`CAL_START` の再送、フェーズ完了待ちのタイムアウト判定を保持する。`ChewingCalibrationFlow` (Presentation/ViewModel) はフェーズの前後関係を判断せず、Model からの呼び出し順に従って案内を表示するだけにする。

### Consequences

- プロトコルの変更 (将来のフェーズ追加や順序変更) が Model 層に閉じ、Presentation 層への影響を最小化できる。
- Presentation 層は「次に何のフェーズが来るか」を Model からの呼び出しでしか知らないため、誤った順序で UI を進める余地がなくなる。

### Traceability

`FR-CC-002`, `FR-CC-004`, `NFR-CC-002`

## ADR-CC-002: 案内表示とカウントダウンは Presentation 層 (`ChewingCalibrationFlow`) に置く

### Context

仕様書 §9.2 はカウントダウン表示を Unity 側の実装必須要件としているが、これは UI 関心事であり、Model 層 (`ChewingSensorService`) に UI ロジックを持たせると、テスト容易性と責務分離 (project.md の View/Model 分離規約) を損なう。

### Decision

案内文言の表示とカウントダウンの進行は `ChewingCalibrationFlow` (Presentation/ViewModel) が持つ。`ChewingSensorService` はカウントダウンの実体を知らず、「フェーズ要求を送る前に完了を待つべき何か」としてのみ扱う。

### Consequences

- UI 文言・カウントダウン秒数の変更が Model 層のコード変更を伴わない。
- Model 層の EditMode テストに UI 依存が入らない。

### Traceability

`FR-CC-003`, `FR-CC-007`, `NFR-CC-001`, `NFR-CC-003`

## ADR-CC-003: `IChewingCalibrationPrompt` を role-specific port として新設する

### Context

Model 層 (フェーズ順序) と Presentation 層 (案内・カウントダウン) を接続する必要があるが、汎用的な `IFetchable`/`IService` のような interface で繋ぐと、project.md の architecture gate (汎用 interface での local/remote・異なる役割の多重 bind 禁止) に抵触し、どのフェーズの待ち合わせかも曖昧になる。

### Decision

`IChewingCalibrationPrompt` という role-specific port を新設する。この port は「各フェーズ要求送信の直前に呼ばれ、await が完了するまで送信を待たせる」契約とし、フェーズ (ノイズ/咀嚼) を区別できるメソッド形状にする。`ChewingSensorService` はこの port の consumer、`ChewingCalibrationFlow` は実装 (adapter) となる。DI 登録は composition root (既存 Installer) に集約する。

### Consequences

- Model はカウントダウンの見た目を知らず、Presentation は仕様書のフェーズ順序を知らないという、双方向の責務漏れを防げる。
- port のメソッドシグネチャがフェーズ追加時の変更点になるため、将来のフェーズ変更時にレビュー対象が明確になる。
- 既存の `Action onAccepted` コールバック方式 (`CalibrateAsync(onAccepted, ct)`) は、この port による2回の待ち合わせ (ノイズ前・咀嚼前) に置き換わるか、`onAccepted` 相当の呼び出しがこの port の最初の呼び出しに統合される。どちらの形にするかはコード設計時に確定し、`CalibrateAsync` のシグネチャ変更として記録する。

### Traceability

`FR-CC-002`, `FR-CC-003`, `FR-CC-004`

## ADR-CC-004: 失敗時もチュートリアルを継続する既存方針をフェーズ分割後も維持する

### Context

v1.0 の `ChewingCalibrationFlow` は、咀嚼計の未接続・失敗・タイムアウトでも例外を投げず案内を閉じて続行する設計になっている。これは無人展示で1台の不調が来場者を足止めしないための意図的な設計であり、フェーズが2つに増えても後退させてはならない。

### Decision

フェーズ分割後も、いずれのフェーズ (`CAL_START` 未受理、ノイズ測定失敗、咀嚼測定失敗、いずれの `CAL_FAILED` 理由、タイムアウト、未接続、中断) でも例外を投げず、結果型で表現し、呼び出し元 (Tutorial) が catch なしに次へ進めるようにする。

### Consequences

- `ChewingCalibrationResult` 相当の型がフェーズ分割後の失敗経路も表現できるよう見直しが必要になる可能性がある。
- 新しい失敗理由 (`NOT_STARTED`) を特別扱いせず、既存の「理由文字列を保持したまま続行」という扱いに統合できる。

### Traceability

`FR-CC-005`, `NFR-CC-006`

## ADR-CC-005: 中断時は `CAL_ABORT` を送信し、切断リセットとは区別する

### Context

仕様書 §9.7 は、Unity が要求を放棄する手段として「`CAL_ABORT` を送る」か「切断してリセットする」の2つを認めている。現行コードには `CAL_ABORT` 相当の送信経路がなく、キャンセル時に単に待ち合わせを打ち切るだけになっている。これでは接続を切らない限りデバイス側にフェーズ状態が残り続ける。

### Decision

キャリブレーションの中断 (利用者の操作放棄、セッションリセット、チュートリアル離脱によるキャンセル) では、接続を切らずに `CAL_ABORT,<requestId>` を送信してデバイス側のフェーズ状態を破棄させる。接続そのものが失われた場合 (I/O 例外・ポート消失) は、既存の切断処理 (仕様書 §14) によるリセットで代替し、`CAL_ABORT` を重ねて送らない。

### Consequences

- 中断後すぐに再度キャリブレーションを開始しても、デバイス側が古いフェーズ状態を保持したままにならない。
- `ChewingSensorService` に「保留要求があるときのキャンセル」と「接続喪失によるキャンセル」を区別する分岐が必要になる。

### Traceability

`FR-CC-006`

## ADR-CC-006: カウントダウン秒数・案内文言は `TutorialConfig` と Localization テーブルの現場調整データとする

### Context

現行の `ChewingCalibrationChewPromptDelaySeconds` は `TutorialConfig` の SerializeField として既に現場調整可能だが、これは「文言差し替えまでの待ち時間」であり、v1.1 では「フェーズ要求を送る前のカウントダウン」という異なる意味を持つ値に置き換わる。文言もノイズ用・咀嚼用に加えて「計測中」表示が必要になる。

### Decision

カウントダウン秒数、ノイズ測定指示文言、咀嚼測定指示文言、計測中表示文言を `TutorialConfig` (ScriptableObject) のフィールドと `TutorialStrings` Localization テーブルのエントリとして管理する。コード内固定値・埋め込み文字列を使わない。新規 Localization キーの追加は Editor メニュー `YummyVerse > Tutorial > Create Default Tutorial Assets` の再実行を運用手順として必須とする。

### Consequences

- 展示現場でカウントダウン秒数や文言を調整する際にコード変更を要しない。
- `TutorialAssetBuilder` の冪等動作 (既存文言を上書きしない) が、新規キー追加時にも安全に働くことを前提にできる。
- SO フィールド追加とメニュー再実行の順序 (フィールドを先に追加してからメニューを実行する) を実装手順として明記する必要がある。

### Traceability

`FR-CC-007`, `NFR-CC-003`

## ADR-CC-007: 実装完了・テスト成功は証拠が追加されるまで宣言しない

### Context

咀嚼計ファームウェアの v1.1 対応状況、各フェーズの実測時間、実機でのフェーズ分割動作は本 intent の記録時点で未確認である。設計文書だけで実装・検証の完了を装うと、`project.md` の「未実施は `NOT-RUN` と記録し、成功扱いにしない」という運用ルールに反する。

### Decision

Construction/Verification のステータスは、実際にコードが変更され、EditMode テストまたは実機確認の結果が得られるまで `PENDING`/`NOT-RUN` のままとする。本 intent の documentation agent は要件・設計の記録のみを完了とし、実装・検証の完了を代わりに宣言しない。

### Consequences

- 後続の実装作業者は `construction/implementation.md` と `verification/test-results.md` を新規に作成し、証拠を追記する必要がある。
- 本 intent の `aidlc-state.md` は実装着手まで `inception-recorded` のまま更新しない。

### Traceability

`NFR-CC-004`, `NFR-CC-005`

## Exceptions

この設計時点で承認済みの architecture exception はない。実装中に必要になった場合は `EX-CC-###` を発行し、`architecture-and-code-quality.md` の手続きに従って期限付きで記録する。
