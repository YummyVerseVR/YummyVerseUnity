# AI-DLC Intent State

- Intent ID: `260824-guided-food-experience`
- Title: ガイド付き食体験・仮想メニュー・食事アクションの要件統合
- Created: 2026-08-24
- Space: `default`
- Operation mode: documentation-only manual workflow
- Status: `requirements-baselined-api-contract-blocked`
- Current phase: Inception

## Executed Stages

- Ideation / intent-capture: `docs` に存在したチュートリアル要件と 2026-08-24 の追加要求を、一つの来場者体験として記録済み。
- Inception / requirements-analysis: `FR1`〜`FR35`、`NFR1`〜`NFR14`、受け入れシナリオ、制約、未解決事項を記録済み。
- Inception / domain-design: 責務境界、体験フロー、失敗経路、ADR、トレーサビリティを記録済み。
- Inception / contract-design: YummyService v2 domain contract、required consumer API capabilities、current transport gaps、client adapter gate を記録済み。
- Verification: API source revision/checksum、v2 OpenAPI readiness、route scan、current Unity gap、requirement/ADR traceability を確認済み。
- Construction / Operation: この intent では未実施。既存実装が全要件を満たすとは判定していない。

## Approval Basis

- 2026-08-24 の利用者要求により、`docs` 削除後も `aidlc` 単体で要件を理解できることが明示された。
- `docs/tutorial-requirement.md` と `docs/tutorial-usage.md` のうち要件・制約・受け入れ条件・運用上不可欠な実装境界を移管した。
- 2026-08-24 の追加要求として、チュートリアル、QR の役割変更、仮想/物理メニュー、画像プレビュー、食事当たり判定、縮小・食べカス・消滅演出を統合した。
- 2026-08-24 の利用者指定により、外部 API は `https://github.com/YummyVerseVR/YummyService` の v2 を使用する。`main@546b455fedd205fb686ca7b93d6af596bced7879` の規範 contract を確認した。
- 2026-08-24 の利用者確認により、Standalone Mode は API 非依存の恒久機能として維持し、Tutorial 完了後の一つの食品選択 UI に v2 API item と local item を統合表示する。

## Supersession

- この intent の `FR12`〜`FR16` と `ADR-003` は、`260821-spatial-anchor-food-placement` における「QR payload/GUID を食品選択に継続利用する」という要件・判断を上書きする。
- 旧 intent は実装当時の履歴として保持する。現行の製品要件を判断するときは本 intent を優先する。

## Readiness

- 要件移管と要件ベースライン: `READY`
- YummyService v2 domain mapping: `READY`。
- YummyService v2 production HTTP integration: `NOT-READY`。現行 v2 OpenAPI は `2.0.0-draft`、`paths: {}`、placeholder server URL で、auth/artifact lookup/download が deferred。
- Construction 開始: `NOT-READY`。`Q1`〜`Q11` のうち対象 Unit に影響する項目を解決し、既存実装との差分計画を作成する必要がある。
