# AI-DLC Intent State

- Intent ID: `260828-architecture-redesign`
- Title: YummyVerseUnity コアロジック・依存関係・View 層の再設計
- Created: 2026-08-28
- Space: `default`
- Operation mode: documentation-only manual workflow
- Status: `construction-in-progress`
- Current phase: Construction (implementation agent in progress)

## Purpose

コアロジックの責務を Domain/Application へ整理し、外部境界ごとの port/adapter、composition root、薄い MonoBehaviour View、lifecycle ownership、未使用コードの証拠付き削除を定義する。既存の製品要件は保持し、コード実装の完了・テスト成功は確認できるまで宣言しない。

## Runtime Root

この intent の初期監査で扱う runtime root は、`ProjectSettings/EditorBuildSettings.asset` で唯一 enabled の `Assets/YummyVerse/Scene/Restaurant.unity`、その再帰 Prefab/asset graph、DI の `NonLazy`/`IInitializable`、Unity lifecycle callbacks、および Editor tests である。

## Stage Status

- Ideation / intent-capture: 設計再発防止の目的、scope、成功条件を記録済み。
- Inception / requirements-analysis: `FR-AR-001`〜`FR-AR-011`、`NFR-AR-001`〜`NFR-AR-011` を固定 ID で記録済み。
- Inception / domain-design: target layer、port、View、DI、lifecycle、削除証拠、ADR、traceability を記録済み。
- Inception / delivery-planning: 実装単位、依存順序、品質 gate、rollback を計画済み。
- Construction: 別の実装 agent が進行中。この文書作成時点では実装完了・削除完了・テスト成功を記録しない。
- Verification: Ideation/Inception の文書 gate のみ実施。コード、Unity Scene/Prefab、Quest、PCVR の結果は未確認。

## Product Decision Preservation

`260821-spatial-anchor-food-placement` と `260824-guided-food-experience` の製品固有決定（Spatial Anchor と anchor-relative pose、QR の anchor designation 専用化、YummyService v2、Standalone source、Tutorial、catalog/preview/model、食事 action）は保持する。再設計によって責務の配置を変更しても、これらの identity/lifecycle policy を変更したとはみなさない。

## Documentation Boundary

この intent の documentation agent は `aidlc/` 配下だけを編集する。`Assets/`、`Packages/`、`ProjectSettings/`、コード、Prefab、Scene は編集しない。実装中の agent が後で確認した変更・テスト結果は、別途証拠を受領してから `construction`/`verification` へ追記する。
