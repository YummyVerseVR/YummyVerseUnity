# Requirements Analysis Memory

## Confirmed

- FR/NFR は `FR-AR-001`〜`FR-AR-011`、`NFR-AR-001`〜`NFR-AR-011` として固定した。
- runtime root、依存 gate、View 規約、port/adapter、DI、lifecycle、unused、asset GUID、test/platform 分離、例外手続きを要求化した。
- 既存製品要件の変更ではなく、責務と品質 gate の再設計である。

## Decisions

- Scene/Prefab attachment 単独は使用根拠にしない。
- Domain/Application は concrete 外部技術を知らず、consumer 側に role-specific port を置く。
- 未実行は `NOT-RUN` とし、baseline build の成功を再設計後の成功に流用しない。

## Open Questions

- 実装後に各 FR/NFR を満たす具体的な class/namespace/assembly の最終形。
- View ごとの serialized field 移行と Prefab/Scene GUID 検証結果。
- unused 候補の削除可否、Editor test/editor tooling の保持理由。
- Quest/PCVR/Unity load の再現可能な検証環境と結果。
