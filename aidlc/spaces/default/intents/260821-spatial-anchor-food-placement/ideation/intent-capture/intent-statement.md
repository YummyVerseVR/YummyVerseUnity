# Intent Statement

## Historical Scope Notice

この文書は 2026-08-21 の intent を記録する。QR payload/GUID を食品選択に残す記述は、`260824-guided-food-experience` の `FR13`/`FR16` と `ADR-003` により superseded された。現行要件では食品 identity は生成履歴メニューから得て、QR は anchor designation のみに使用する。Spatial Anchor placement の非競合部分は履歴・実装基準として維持する。

## Problem

現行の食べ物 3D モデルは QR trackable の Transform へ追従するため、QR の検出状態・置き方・追跡揺れが食べ物の表示位置へ直接影響する。展示運営者が表示位置を明示的に調整し、端末再起動後も同じ物理空間へ復元できる配置基準が必要である。

## Desired Outcome

- コントローラーのボタンから設定画面を開ける。
- 設定画面で draft Cube を配置し、その現在 pose に Meta Spatial Anchor を作成または置換できる。
- Anchor 保存後、展示運営者が Anchor 自体を動かさずに Cube を掴んで食べ物の表示 pose を調整し、確定できる。
- 確定した Anchor UUID と Cube の anchor-relative pose を保存し、次回起動時に復元できる。
- QR は通常モードの食べ物 GUID 入力に限定され、QR の移動・ロストが食べ物の表示 pose を変化させない。

## Primary Actor

- 展示運営者

## Scope

- Meta Quest 3 上の Spatial Anchor の作成、保存、読み込み、localization。
- 設定画面の表示、アンカー操作、配置編集、確定、再設定、状態表示。
- XR Controller で掴める設定用 Cube。
- 食べ物表示 pose の供給元を QR Transform から Spatial Anchor 配置設定へ変更。
- Anchor UUID と anchor-relative pose の永続化および復旧フロー。

## Out of Scope

- QR を用いた食べ物 GUID 選択そのものの廃止。
- Yummy Control Server、GLB download、Standalone food file の契約変更。
- Food Scale やチュートリアル内容の変更。
- 複数 Anchor または複数の食べ物表示位置の同時管理。
- Cloud Anchor 共有および端末間の設定同期。

## Success Criteria

1. 設定済みの端末を再起動して Anchor が localization された後、食べ物が保存済み anchor-relative pose に表示される。
2. 配置確定後に QR を移動またはロストさせても、食べ物の world pose が QR に追従しない。
3. 保存済み Anchor を読み込めない場合、古い world pose を適用せず、設定画面から再設定できる。

## Terminology

- 製品・コード・文書では Meta SDK の正式表記に合わせて `Spatial Anchor` を用いる。要求文中の `Spacial Anchor` は同じ概念を指すものとして扱う。
