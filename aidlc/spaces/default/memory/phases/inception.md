# Inception Phase Guardrails

## Requirements Quality

- 機能要件は安定 ID と検証可能な pass/fail 条件を持つ。
- 性能、可用性、操作時間、タイムアウトは可能な限り数値で示す。
- Quest、PCVR、Standalone Mode、ネットワーク接続時の適用範囲を曖昧にしない。

## Architecture Standards

- 重要な設計判断は Context、Decision、Consequences、Alternatives Rejected を含む ADR として残す。
- Unity lifecycle、DI scope、非同期キャンセル、ScriptableObject の共有状態を設計時に確認する。
- 新しい外部依存または Package は、既存選択との比較と導入理由を記録する。

## Traceability

- `FR`, `NFR`, user story, Unit, plan step, test result の対応を維持する。
- 既存仕様と矛盾する新要件は、出典と承認を記録するまで確定扱いにしない。

## Corrections
