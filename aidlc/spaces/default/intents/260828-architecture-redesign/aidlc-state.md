# AI-DLC Intent State

- Intent ID: `260828-architecture-redesign`
- Title: YummyVerseUnity コアロジック・依存関係・View 層の再設計
- Created: 2026-08-28
- Space: `default`
- Operation mode: documentation-only manual workflow
- Status: `construction-code-complete`
- Current phase: Construction 完了、Verification は `G-01` と静的検査のみ実施

## Purpose

コアロジックの責務を Domain/Application へ整理し、外部境界ごとの port/adapter、composition root、薄い MonoBehaviour View、lifecycle ownership、未使用コードの証拠付き削除を定義する。既存の製品要件は保持し、コード実装の完了・テスト成功は確認できるまで宣言しない。

## Runtime Root

この intent の初期監査で扱う runtime root は、`ProjectSettings/EditorBuildSettings.asset` で唯一 enabled の `Assets/YummyVerse/Scene/Restaurant.unity`、その再帰 Prefab/asset graph、DI の `NonLazy`/`IInitializable`、Unity lifecycle callbacks、および Editor tests である。

## Stage Status

- Ideation / intent-capture: 設計再発防止の目的、scope、成功条件を記録済み。
- Inception / requirements-analysis: `FR-AR-001`〜`FR-AR-011`、`NFR-AR-001`〜`NFR-AR-011` を固定 ID で記録済み。
- Inception / domain-design: target layer、port、View、DI、lifecycle、削除証拠、ADR、traceability を記録済み。
- Inception / delivery-planning: 実装単位、依存順序、品質 gate、rollback を計画済み。
- Construction: `UNIT-AR-01`〜`UNIT-AR-07` のコード変更と証拠付き削除を完了。`UNIT-AR-08` は部分実施。詳細は `construction/implementation.md`。
- Verification: `G-01`（C# compile）と静的 asset/依存検査が `PASS`。`G-02`〜`G-06` は `NOT-RUN`。詳細は `verification/test-results.md`。

## Product Decision Preservation

`260821-spatial-anchor-food-placement` と `260824-guided-food-experience` の製品固有決定（Spatial Anchor と anchor-relative pose、QR の anchor designation 専用化、YummyService v2、Standalone source、Tutorial、catalog/preview/model、食事 action）は保持する。再設計によって責務の配置を変更しても、これらの identity/lifecycle policy を変更したとはみなさない。

## Documentation Boundary

設計・要件を記録した documentation agent は `aidlc/` 配下だけを編集した。`Assets/` のコード・Prefab・Scene 変更は実装側が行い、その結果を証拠付きで `construction`/`verification` へ追記している。実機・Editor 実行を伴う gate は、実際に実行した証拠を受領するまで `NOT-RUN` のままにする。
