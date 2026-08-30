# AI-DLC Intent State

- Intent ID: `260824-guided-food-experience`
- Title: ガイド付き食体験・仮想メニュー・食事アクションの要件統合
- Created: 2026-08-24
- Space: `default`
- Operation mode: documentation-only manual workflow
- Status: `partial-construction-ready-units-implemented`
- Current phase: Construction (partial)

## Executed Stages

- Ideation / intent-capture: `docs` に存在したチュートリアル要件と 2026-08-24 の追加要求を、一つの来場者体験として記録済み。
- Inception / requirements-analysis: `FR1`〜`FR35`、`NFR1`〜`NFR14`、受け入れシナリオ、制約、未解決事項を記録済み。
- Inception / domain-design: 責務境界、体験フロー、失敗経路、ADR、トレーサビリティを記録済み。
- Inception / contract-design: YummyService v2 domain contract、Unity Device の現行 routes/schemas、required consumer API capabilities、current transport gaps、client adapter gate を記録済み。
- Verification: API source revision/checksum、v2 OpenAPI readiness、route scan、current Unity gap、requirement/ADR traceability を確認済み。
- Construction: 利用者が明示承認した `UNIT-01`〜`UNIT-03` のみ実装・検証済み。`UNIT-04`〜`UNIT-08` は未実施。
- Operation: 未実施。既存実装と今回の partial delivery が全要件を満たすとは判定していない。

## Approval Basis

- 2026-08-24 の利用者要求により、`docs` 削除後も `aidlc` 単体で要件を理解できることが明示された。
- `docs/tutorial-requirement.md` と `docs/tutorial-usage.md` のうち要件・制約・受け入れ条件・運用上不可欠な実装境界を移管した。
- 2026-08-24 の追加要求として、チュートリアル、QR の役割変更、仮想/物理メニュー、画像プレビュー、食事当たり判定、縮小・食べカス・消滅演出を統合した。
- 2026-08-30 の更新により、外部 API は `https://github.com/YummyVerseVR/YummyService` の `ru322/main@97c9ed75980ec398fe75159bd4e011b489112433` を規範 snapshot とする。OpenAPI は104 paths/124 schemasで、旧 `main@546b455...` の `paths: {}` snapshot は superseded された。
- 2026-08-24 の利用者確認により、Standalone Mode は API 非依存の恒久機能として維持し、Tutorial 完了後の一つの食品選択 UI に v2 API item と local item を統合表示する。

## Supersession

- この intent の `FR12`〜`FR16` と `ADR-003` は、`260821-spatial-anchor-food-placement` における「QR payload/GUID を食品選択に継続利用する」という要件・判断を上書きする。
- 旧 intent は実装当時の履歴として保持する。現行の製品要件を判断するときは本 intent を優先する。

## Readiness

- 要件移管と要件ベースライン: `READY`
- YummyService v2 domain mapping: `READY`。
- YummyService v2 Unity Device route/schema: `READY`。history/status/artifact/payload/ACK と `deviceBearerAuth` が公開済み。
- YummyService v2 full consumer/production HTTP integration: `NOT-READY`。placeholder server URL、preview、全 stage/detail、Unity artifact checksum、deployment/TLS/secret delivery、runtime compatibility negotiation が未解決。
- `UNIT-01`〜`UNIT-03` partial Construction: `IMPLEMENTED-VERIFIED`。
- Intent-wide Construction completion: `NOT-READY`。`Q1`〜`Q10`、normative v2 の preview/full-stage/checksum/deployment gaps、scene/device-specific decisions を解決する必要がある。`Q11` は completed order の selected verified output のみ downloadable とする方針で解決済み。
