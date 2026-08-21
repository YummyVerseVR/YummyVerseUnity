# AI-DLC Intent State

- Intent ID: `260821-spatial-anchor-food-placement`
- Title: Spatial Anchor を基準とする食べ物表示位置の設定
- Created: 2026-08-21
- Space: `default`
- Operation mode: documentation-only manual workflow
- Status: `awaiting-device-verification`
- Current phase: Verification

## Executed Stages

- Ideation / intent-capture: intent と対象範囲を記録済み。
- Inception / requirements-analysis: `FR1`〜`FR10`、`NFR1`〜`NFR5` を記録済み。
- Inception / domain-design: コンポーネント、状態遷移、永続化形式、ADR、トレーサビリティを記録済み。
- Construction: QR/placement 分離、Spatial Anchor backend、永続化、設定 UI、grab Cube、表示 gate を実装済み。
- Verification: Unity compile、Editor/PlayMode smoke test、unsupported provider failure path を確認済み。Quest 3 実機受入は未実施。

## Approval Basis

- 2026-08-21 の利用者要求により、QR Transform から Spatial Anchor への表示位置基準変更と実装が明示的に依頼された。
- Anchor UUID と Cube の anchor-relative pose を保存し、次回起動時に復元する方針を今回の実装判断として採用した。
- 実装と Unity Editor 検証の結果、および Quest 3 実機未検証項目は Construction/Verification 成果物に記録する。
