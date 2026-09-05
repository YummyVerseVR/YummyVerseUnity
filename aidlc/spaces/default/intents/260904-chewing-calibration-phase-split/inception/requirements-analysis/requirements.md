# Requirements

## Intent Analysis

目的は、咀嚼計シリアル通信プロトコル v1.1 (`YummyVerse_Serial_Protocol_v1.1.md`, `YV-SERIAL-001`) のキャリブレーション・フェーズ分割に Unity 側 (`ChewingSensorService`/`ChewingCalibrationFlow`) を対応させ、仕様書 §9.2 が実装必須とするカウントダウン要件、`CAL_ABORT` による中断、失敗時もチュートリアルを止めない既存方針を満たす設計を確定することである。現行コードは v1.0 形状 (一括測定 + 固定タイマーによる文言差し替え) のままであり、その事実は `spaces/default/knowledge/aidlc-shared/chewing-sensor-serial-protocol.md` に記録済みである。

以下の ID はこの intent の恒久 ID であり、Construction/Verification で変更・再採番しない。要求の実装状況は、証拠が追加されるまで `PENDING` とする。

## Functional Requirements

### FR-CC-001: フェーズ分割コマンドの構築・解釈に対応する (`MUST`)

`ChewingSensorProtocol` は `CAL_NOISE,<requestId>`/`CAL_CHEW,<requestId>`/`CAL_ABORT,<requestId>` の構築と、`CAL_NOISE_DONE,<requestId>`/`CAL_CHEW_DONE,<requestId>` の解釈に対応すること。`CAL_FAILED` の理由に `NOT_STARTED`/`ABORTED` が現れても既存の自由文字列解釈で受理できることを確認すること。

検証条件:

- 新規コマンドの構築結果が仕様書 §8 の形式 (`COMMAND,<requestId>`) と一致する。
- `TryParse` が新規完了応答を正しい `ChewingSensorMessageKind` として解釈する。
- 命令名は既存同様に大文字・小文字を区別し、不正な形式は例外を投げず破棄する。

初期状態: `PENDING`。

### FR-CC-002: フェーズ順序・再送・タイムアウトを Model 層に保持する (`MUST`)

`ChewingSensorService` は `CAL_START` → `CAL_ACCEPTED` → `CAL_NOISE` → `CAL_NOISE_DONE` → `CAL_CHEW` → `CAL_CHEW_DONE` → `CAL_DONE` の状態遷移、`CAL_START` の再送、フェーズ完了待ちのタイムアウト判定を保持すること。フェーズ順序の判断を ViewModel/Presentation 層へ漏らさないこと。

検証条件:

- フェーズの前後関係 (ノイズ→咀嚼) を Model 層のコードだけで判定できる。
- 同じ `requestId` の重複応答・再接続時の世代不一致 (`_connectionEpoch` 相当) が既存方針 (仕様書 §10) のまま扱われる。
- フェーズ完了待ちのタイムアウトが設定値 (`ChewingSensorConfig`) から決定できる。

初期状態: `PENDING`。

### FR-CC-003: 各フェーズ要求の前にカウントダウンを表示し、0 で送信する (`MUST`)

仕様書 §9.2 の実装必須要件どおり、`CAL_NOISE`/`CAL_CHEW` を送信する前に、Presentation 層が案内文言とカウントダウンを表示し、カウントが 0 になった時点で当該フェーズの要求を送信すること。画面フローは次の通りとする。

1. 「小さく歯をカチカチしてください」+ 5秒カウントダウン → 0 で「計測中...」に切り替わると同時に `CAL_NOISE` を送信する。
2. `CAL_NOISE_DONE` 受信後、「奥歯でちゃんと噛みしめてください」+ 5秒カウントダウン → 0 で「計測中...」+ `CAL_CHEW` を送信する。
3. `CAL_DONE` 受信でキャリブレーション案内を閉じ、既存のチュートリアル本体 (S2「YummyVerse へようこそ」) へ遷移する。

検証条件:

- カウントダウンが 0 になるまでフェーズ要求が送信されないことをコード上の待ち合わせ (await) で保証できる。
- カウントダウン中の表示と「計測中」表示が仕様書 §9.2 の例と対応している。
- カウントダウン秒数は固定値ではなく `TutorialConfig` から取得する (FR-CC-006 参照)。

初期状態: `PENDING`。

### FR-CC-004: role-specific port で Model と Presentation を接続する (`MUST`)

Model 層 (`ChewingSensorService`) と Presentation 層 (`ChewingCalibrationFlow`) を、汎用的な interface ではなく role-specific port `IChewingCalibrationPrompt` で接続すること。この port は各フェーズ要求送信の直前に呼ばれ、await が完了するまで送信を待たせる契約とすること。

検証条件:

- port のメソッドがフェーズ (ノイズ/咀嚼) を区別できる形になっている。
- `ChewingSensorService` は port の実装がどのように待つか (UI 表示内容) を知らない。
- `ChewingCalibrationFlow` は port を実装するが、フェーズ順序そのものは判断しない (Model からの呼び出し順に従うだけ)。
- project.md の architecture gate (汎用 `IFetchable`/`IService` の多重 bind 禁止) に抵触しない。

初期状態: `PENDING`。

### FR-CC-005: 失敗時もチュートリアルを継続する (`MUST`)

咀嚼計の未接続・いずれかのフェーズでの失敗・タイムアウトが発生しても例外を投げず、キャリブレーション案内を閉じてチュートリアル (S2 以降) を続行すること。これは v1.0 から不変の方針であり、フェーズ分割後も同一の観測可能な振る舞いを維持すること。

検証条件:

- `CAL_FAILED` の理由 (`BUSY`/`NOT_STARTED`/`ABORTED`/`SENSOR_ERROR`/`SENSOR_UNSTABLE`/`INTERNAL_ERROR`) のいずれでも、呼び出し元 (Tutorial 側) が中断されず先へ進む。
- 咀嚼計と一度も接続できない場合も同様に先へ進む。
- 例外ではなく結果型 (`ChewingCalibrationResult` 相当の拡張) で失敗を表現する既存方針を維持する。

初期状態: `PENDING`。

### FR-CC-006: 中断時に `CAL_ABORT` を送信する (`MUST`)

利用者の操作放棄・セッションリセット・チュートリアル離脱によってキャリブレーションが中断される場合、Unity は保留中の `requestId` で `CAL_ABORT` を送信し、デバイス側のフェーズ状態を破棄させること。

検証条件:

- 中断経路 (`CancellationToken` のキャンセル等) が `CAL_ABORT` の送信につながる。
- 保留中の要求がない状態での中断では `CAL_ABORT` を送らない (無駄な送信をしない)。
- `CAL_ABORT` 送信後に `CAL_FAILED,<requestId>,ABORTED` を受けても例外にならない。

初期状態: `PENDING`。

### FR-CC-007: カウントダウン秒数・案内文言を現場調整可能にする (`MUST`)

カウントダウンの秒数と各フェーズの案内文言 (ノイズ測定指示、咀嚼測定指示、「計測中」表示) を `TutorialConfig` (ScriptableObject) の設定値と `TutorialStrings` Localization テーブルで現場調整可能にすること。

検証条件:

- `TutorialConfig` に新規フィールドを追加する場合、既存の `ChewingCalibrationChewPromptDelaySeconds` のようなコード内固定値に依存しない。
- 新規 Localization キーの追加は Editor メニュー `YummyVerse > Tutorial > Create Default Tutorial Assets` の再実行を必要とすることが運用手順として記録されている。
- 当該 Editor メニューの生成処理は既存 asset/文言を上書きしない冪等動作を維持する (`tutorial-system.md` の既存記述と矛盾しない)。

初期状態: `PENDING`。

## Non-Functional Requirements

### NFR-CC-001: メインスレッドをブロックしない (`MUST`)

フェーズ分割後も `ChewingSensorService` はメインスレッドをシリアル I/O でブロックしないこと (仕様書 §15.1)。role-specific port の待ち合わせ (UI 表示・カウントダウン) はメインスレッドの通常フレーム処理を妨げない非同期処理であること。

検証方法: 既存のスレッド分離設計 (受信専用スレッド + `Tick` によるメインスレッド処理) を変更しないことをコードレビューで確認する。

### NFR-CC-002: フェーズ単位でタイムアウトを設定可能にする (`MUST`)

`CAL_NOISE_DONE`・`CAL_CHEW_DONE` それぞれの完了待ちタイムアウトを独立して設定可能にすること。単一の `CalibrationCompletionTimeoutSeconds` で2フェーズ分をまとめて表現しないこと。

検証方法: `ChewingSensorConfig` のフィールド設計レビュー。フェーズごとに異なるタイムアウト値を設定できることを確認する。

### NFR-CC-003: 現場調整の追加が既存文言を破壊しない (`MUST`)

新規 Localization キー・`TutorialConfig` フィールドの追加が、既存のチュートリアル文言・進行に影響しないこと。

検証方法: `TutorialAssetBuilder` の冪等動作 (既存 asset/文言を上書きしない) をレビューし、新規キー追加後の既存キー差分がないことを確認する。

### NFR-CC-004: プロトコル文字列の EditMode 回帰を維持する (`MUST`)

新規コマンド・応答の構築・解釈が `ChewingSensorProtocolTests` で Unity Editor/デバイスなしに検証可能であること。

検証方法: `Editor/Tests/ChewingSensorProtocolTests.cs` への新規テストケース追加レビュー。

### NFR-CC-005: 実行結果を混同しない (`MUST`)

EditMode テスト結果、Unity Editor load、実機 (Quest/PCVR/Standalone) でのフェーズ分割動作確認を分離して記録し、未実施を成功として扱わないこと。

検証方法: `verification/test-results.md` (実装側が作成) での `NOT-RUN`/`PASS`/`FAIL` の明示。

### NFR-CC-006: 例外に頼らず結果型で失敗を表現する (`MUST`)

フェーズ分割後もキャリブレーションの失敗経路 (未接続・タイムアウト・各フェーズの `CAL_FAILED`・中断) を例外ではなく結果型で表現し、呼び出し元が catch なしに分岐できること。

検証方法: `ChewingCalibrationResult` 相当の型のケース網羅レビュー、呼び出し元 (`ChewingCalibrationFlow`) の try/catch 依存の有無確認。

## Constraints and Baseline

- Unity Editor 基準は `ProjectSettings/ProjectVersion.txt` の `6000.2.6f2`。C#、UniTask、R3、Extenject の既存選択を尊重する。
- プロトコルの規範的なソースは `YummyVerse_Serial_Protocol_v1.1.md` (`YV-SERIAL-001`) とし、本 intent はその複製ではなく実装判断だけを記録する。
- documentation agent は `aidlc/` 以外を編集しない。コード実装、Editor 資産変更、実機検証は実装作業者と後続 verification の責務である。
- `project.md` の architecture 規約 (role-specific port、read-only state/command 分離、composition root への DI 集約) を継承する。

## Status

全 FR/NFR は設計の gate として固定した。実装・削除完了・テスト成功は未確認であり、`PENDING`/`NOT-RUN` のまま扱う。
