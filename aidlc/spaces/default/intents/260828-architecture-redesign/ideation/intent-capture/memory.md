# Ideation Memory

## Confirmed

- 2026-08-28 の依頼は、core refactoring、class dependency redesign、thin View、unused code removal、再発防止規約の文書化を一つの coherent intent として扱う。
- runtime root は active build scene とその asset graph、DI activation、Unity callback、Editor tests である。
- 既存の製品固有要件は保持し、architecture redesign は実装責務と依存方向を対象にする。

## Decisions

- 設計規約は space-level knowledge と `memory/project.md` の両方に置き、intent には今回の適用証拠を置く。
- `intents.json` は存在しないため作成しない。
- 実装 agent が進行中のため、construction/verification は初期監査・計画・未確認事項に限定する。

## Open Questions

- 実装後の最終到達性 graph と削除対象。
- View ごとの collaborator 抽出境界、Prefab/Scene serialized reference の更新結果。
- 各 port の最終 interface 名と DI scope。
- Quest/PCVR の device-specific behavior と Unity load の結果。
