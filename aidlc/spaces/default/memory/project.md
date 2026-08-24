# Project-Level Rules

## Tech Stack

- Unity Editor の基準は `ProjectSettings/ProjectVersion.txt` に記録された `6000.2.6f2` とする。
- 実装言語は C#。非同期処理は UniTask、リアクティブ処理は R3、依存性注入は Extenject を既存選択として尊重する。
- 対象は Meta Quest 3 を中心とする Android/OpenXR と PCVR、および iPad 等の外部 viewer。MR/Spatial Anchor、コントローラー操作、QR による anchor designation、生成食品 catalog、preview/model 取得、展示運用を主要境界として扱う。
- Unity Package の版は `Packages/manifest.json` と `Packages/packages-lock.json` を根拠にする。

## Architecture

- `Assets/YummyVerse/Scripts/Model`, `ViewModel`, `View` の責務分離と、Interface 経由の依存方向を維持する。
- DI 登録は既存 Installer に集約し、ViewModel/Model から Unity View の具象へ直接依存させない。
- チュートリアルは ScriptableObject によるデータ駆動、イベント購読、`CancellationToken` の一括伝播という既存方針を維持する。
- Spatial Anchor、永続化、XR grab interaction、QR、ネットワーク、ファイル、入力、シーン、端末は統合境界として扱い、失敗と復旧動作を要件・テストに含める。
- 食べ物の識別と表示 pose を分離する。現行 target では食品 identity は生成履歴メニューの item ID から得て、QR はモデル出現 anchor の designation にだけ使う。QR payload/GUID を食品生成・選択・model download key に使用しない。
- 食べ物の表示 pose は、永続化した Spatial Anchor UUID と設定用 Cube の anchor-relative pose を単一の設定として扱う。
- メニュー一覧は preview image/metadata を先行取得し、全 3D model を一覧表示のために先行 load しない。
- Food interaction は game event の発火責任を持ち、Tutorial は FoodScooped/DishCleared を購読するだけにする。

## Testing Posture

- 変更対象に対する EditMode または PlayMode テストを検討し、追加しない場合は理由と代替確認を `test-results.md` に残す。
- Unity Editor 上の自動テストと、Quest/PCVR の実機確認を混同しない。それぞれの結果と未実施理由を分けて記録する。
- 展示セッションのリセット、ユーザー離脱、Anchor の保存・復元失敗、保存データの欠落・破損、設定用 Cube の編集・固定、QR ロスト、通信失敗、ローカルファイル欠落を主要な回帰観点とする。
- QR の payload/認識を food identity source として再導入していないこと、designation 後の一時的ロストで食品が誤配置されないことを回帰確認する。
- Menu 表示時に未選択 3D model が load されないこと、scoop event の one-shot、visual/collider の段階縮小、crumb/disappear、DishCleared one-shot を回帰確認する。
- VR menu と physical viewer の item identity/状態整合性を、対象 device/transport が確定した後に確認する。

## Documentation

- 製品要件の規範的なソースは `aidlc/spaces/default/intents/`、intent をまたぐ安定知識は `aidlc/spaces/default/knowledge/aidlc-shared/` とする。
- チュートリアル、menu/model、QR/anchor、viewer、eating action の現行要件は `260824-guided-food-experience` を参照する。
- `docs/` は移管完了後の現行要件ソースとして参照しない。要件は source path の存在なしに理解できる本文と恒久 ID を `aidlc` に持たせる。
- 実装と `aidlc` の不一致を発見した場合、黙って片方を正とせず gap/未解決事項として記録する。
- `README.md` の Unity `6000.2.0f1` 表記と `ProjectVersion.txt` の `6000.2.6f2` は不一致のため、関連 intent で解消するまで既知の差分として扱う。

## Scope Overrides

- Unity が自動生成するファイル、Package キャッシュ、Build 成果物は設計対象または明示的な依頼でない限り変更しない。
- `.unity`, `.prefab`, `.asset` の変更はテキスト差分だけで完全性を断定せず、可能な範囲で Unity Editor のロードまたは検証結果を残す。

## Decided

- DECIDED: AI-DLC 成果物は V2 の space/intent モデルで管理し、旧 `aidlc-docs/` 構造を新規採用しない (2026-08-16)。
- DECIDED: 通常の作業 space は `default` とする (2026-08-16)。
- SUPERSEDED: 食べ物の表示 pose は QR Transform ではなく Spatial Anchor と anchor-relative pose で決定し、QR は通常モードの食べ物 GUID 入力として継続する (2026-08-21)。QR の食品 GUID 責務は 2026-08-24 の決定で上書きされた。配置 pose に関する部分は継続する。
- DECIDED: 設定確定時に Anchor UUID と設定用 Cube の anchor-relative pose を永続化し、次回起動時に復元する (2026-08-21)。
- DECIDED: 食品 identity は生成履歴の仮想メニュー item から得て、QR はモデル出現 anchor の designation のみに使う (2026-08-24)。
- DECIDED: メニュー preview は image/metadata のみを先行取得し、3D model data は選択 item に限定して cache/retrieve/load する (2026-08-24)。
- DECIDED: 現行要件は `aidlc` 内で自己完結させ、`docs/` の存在を要件理解の前提にしない (2026-08-24)。

## Forbidden

- NEVER `Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `UserSettings/` を設計上のソースまたはコミット対象として扱う。
- NEVER Unity の `.meta` と対応アセットの関係を無視して移動・削除する。

## Mandated

- ALWAYS シーンまたは Prefab の参照変更では、DI コンテナ境界と serialized reference の影響を確認する。
- ALWAYS 外部 endpoint と端末パスを扱うときは、タイムアウト、欠落、無効値、復旧を検討する。
- ALWAYS Spatial Anchor の作成、保存、読み込み、localization の各失敗を区別し、無効な保存 pose を world pose として黙って適用しない。
- ALWAYS Anchor を置き換える場合は UUID と anchor-relative pose を一貫して更新し、旧 Anchor の扱いを明示する。
- ALWAYS QR、menu item、preview、model data、anchor placement の各 identity/lifecycle を混同せず、変換境界を明示する。
- ALWAYS session reset 対象と session をまたいで保持する catalog/cache/placement を区別する。

## Corrections

<!-- プロジェクト固有の承認済み学習だけを追記する。 -->
