# Partial Delivery Plan

## Approval Basis

- 利用者指示: 2026-08-24 に、現行 `aidlc` で決定済みの要件だけを実装するよう明示された。
- Delegation: 利用者指定に従い、`UNIT-01` のコード作成を Luna/max subagent へ部分委譲し、primary agent が統合 review と検証を担当する。
- Scope gate: `Q1`〜`Q11`、当時未公開だった v2 HTTP operation、scene/device 固有の未確定事項は実装しない。2026-08-30 refresh 後は公開された Unity Device schema だけを根拠に後続 Unit を再計画する。

## Sequence

1. `UNIT-01`、`UNIT-02`、`UNIT-03` を独立に実装する。
2. Runtime source scan で旧 v1/legacy GUID route と QR→food-load trigger が残っていないことを確認する。
3. Unity 6000.2.6f2 で C# compile と EditMode 相当の unit checks を実行する。
4. 実装 summary、test result、未実装 Unit を Construction/Verification に記録する。

## Delivery Boundary

- この delivery は application/domain foundation と禁止経路の撤去であり、YummyService v2 production integration の提供ではない。
- Unified Virtual Menu、Network catalog、artifact download/cache、Anchor designation integration、AABB/scoop/effect、Physical Viewer は提供対象外。今回の refresh は contract documentation のみで、runtime adapter は変更しない。
- Scene wiring、Quest build、deployment は行わない。

## Risk and Rollback

- `UNIT-01`: domain 型は新規ファイルに隔離する。問題時は新規型と test のみを取り除ける。
- `UNIT-02`: 旧 downloader 削除で Network mode は明示的 unavailable になる。これは v2 contract 不在時の意図した fail-closed であり、旧 endpoint へ戻す rollback は禁止する。必要なら Standalone selection event 配線だけを個別に戻し、outbound route は復元しない。
- `UNIT-03`: 既存 View に未接続の pure state なので、問題時は新規型と test のみを取り除ける。

## Status

- Plan: `APPROVED-FOR-READY-UNITS`
- Ready Unit delivery: `IMPLEMENTED-VERIFIED`
- Full intent delivery: `NOT-READY`
