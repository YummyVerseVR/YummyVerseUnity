# AI-DLC Intent State

- Intent ID: `260904-chewing-calibration-phase-split`
- Title: 咀嚼計キャリブレーションのフェーズ分割対応 (シリアルプロトコル v1.1)
- Created: 2026-09-04
- Space: `default`
- Operation mode: documentation-only manual workflow
- Status: `construction-code-complete`
- Current phase: Construction。コード変更と asset 変更を実施済み (`construction/implementation.md`)。Verification は `NOT-RUN` (`verification/test-results.md`)。

## Purpose

咀嚼計シリアル通信プロトコルが v1.0 から v1.1 (`YummyVerse_Serial_Protocol_v1.1.md`、文書ID `YV-SERIAL-001`) へ更新され、キャリブレーションが `CAL_NOISE`/`CAL_CHEW` の2フェーズへ分割された。従来は `CAL_START` の受理後に咀嚼計が案内なしで一括測定していたため、利用者が「いつ何をすればよいか」分からず、指示前に測定が始まってしまう問題があった。この intent は、Unity 側 (`ChewingSensorService`/`ChewingCalibrationFlow`) がフェーズ分割・カウントダウン必須要件 (仕様書 §9.2)・`CAL_ABORT`・失敗時続行方針を満たすための要件と設計判断を記録する。実装そのものは本 intent の documentation agent の作業範囲外であり、後続の実装作業者が `construction/`・`verification/` を追記する。

## Product Decision Preservation

`260824-guided-food-experience` が定める Tutorial のデータ駆動方針・失敗時続行方針 (咀嚼計の不調でも展示を止めない)、`project.md` の Model/ViewModel/View 責務分離・role-specific port 規約・read-only state/command 分離は変更しない。本 intent はプロトコル v1.1 対応という単一の変更範囲に限定する。

## Stage Status

- Ideation / intent-capture: 目的・scope・成功条件を記録済み (`ideation/intent-capture/intent-statement.md`)。
- Inception / requirements-analysis: `FR-CC-001`〜`FR-CC-007`、`NFR-CC-001`〜`NFR-CC-006` を固定 ID で記録済み (`inception/requirements-analysis/requirements.md`)。
- Inception / domain-design: `ADR-CC-001`〜`ADR-CC-007` を記録済み (`inception/domain-design/decisions.md`)。components/traceability/delivery-planning は未作成。実装着手時に必要となった時点で追加する。
- Construction: コード変更・asset 変更を実施済み。内容と対応 ID は `construction/implementation.md`。
- Verification: compile gate は `PASS`。EditMode テスト実行と、咀嚼計を装着した状態での通し確認 (`CAL_NOISE_DONE` 以降) は `NOT-RUN` (`verification/test-results.md`)。

## Documentation Boundary

この intent の記録は `aidlc/` 配下のみを対象とし、`Assets/`、`README.md`、`YummyVerse_Serial_Protocol_v1.1.md`、`ProjectSettings/` を変更しない。コード実装、`TutorialConfig`/`ChewingSensorConfig` へのフィールド追加、`TutorialStrings` Localization テーブルへの文言追加とそれに伴う Editor メニュー `YummyVerse > Tutorial > Create Default Tutorial Assets` の再実行、実機確認は、本 intent を参照する実装作業者が行い、その結果を証拠付きで `construction/`・`verification/` へ追記する。

## Unresolved Items

- 咀嚼計ファームウェアは 2026-09-04 時点で v1.1 のフェーズ応答 (`CAL_NOISE_DONE`/`CAL_CHEW_DONE`) を返さず、`CAL_NOISE` に対して v1.0 相当の `CAL_DONE` を返す。`CAL_ABORT`/`NOT_STARTED` の対応状況は未確認 (`verification/test-results.md`)。
- ノイズ測定・咀嚼測定それぞれの実測時間は未確認。
- カウントダウン秒数・案内文言の現場最適値は未確定。
- 実機接続でのフェーズ分割シーケンス確認、Editor/Quest/PCVR の実行結果は `NOT-RUN`。
- 詳細な未解決事項は `spaces/default/knowledge/aidlc-shared/chewing-sensor-serial-protocol.md` の「未解決事項」節を参照する。
