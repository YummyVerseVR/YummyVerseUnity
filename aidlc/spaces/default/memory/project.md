# Project-Level Rules

## Tech Stack

- Unity Editor の基準は `ProjectSettings/ProjectVersion.txt` に記録された `6000.2.6f2` とする。
- 実装言語は C#。非同期処理は UniTask、リアクティブ処理は R3、依存性注入は Extenject を既存選択として尊重する。
- 対象は Meta Quest 3 を中心とする Android/OpenXR と PCVR。MR/QR 認識、GLB 取得、展示運用を主要境界として扱う。
- Unity Package の版は `Packages/manifest.json` と `Packages/packages-lock.json` を根拠にする。

## Architecture

- `Assets/YummyVerse/Scripts/Model`, `ViewModel`, `View` の責務分離と、Interface 経由の依存方向を維持する。
- DI 登録は既存 Installer に集約し、ViewModel/Model から Unity View の具象へ直接依存させない。
- チュートリアルは ScriptableObject によるデータ駆動、イベント購読、`CancellationToken` の一括伝播という既存方針を維持する。
- QR、ネットワーク、ファイル、入力、シーン、端末は統合境界として扱い、失敗と復旧動作を要件・テストに含める。

## Testing Posture

- 変更対象に対する EditMode または PlayMode テストを検討し、追加しない場合は理由と代替確認を `test-results.md` に残す。
- Unity Editor 上の自動テストと、Quest/PCVR の実機確認を混同しない。それぞれの結果と未実施理由を分けて記録する。
- 展示セッションのリセット、ユーザー離脱、QR ロスト、通信失敗、ローカルファイル欠落を主要な回帰観点とする。

## Documentation

- 既存の利用者向け概要は `README.md`、チュートリアル仕様は `docs/tutorial-requirement.md`、セットアップと運用は `docs/tutorial-usage.md` を参照する。
- 実装とドキュメントの不一致を発見した場合、黙って片方を正とせず未解決事項として記録する。
- `README.md` の Unity `6000.2.0f1` 表記と `ProjectVersion.txt` の `6000.2.6f2` は不一致のため、関連 intent で解消するまで既知の差分として扱う。

## Scope Overrides

- Unity が自動生成するファイル、Package キャッシュ、Build 成果物は設計対象または明示的な依頼でない限り変更しない。
- `.unity`, `.prefab`, `.asset` の変更はテキスト差分だけで完全性を断定せず、可能な範囲で Unity Editor のロードまたは検証結果を残す。

## Decided

- DECIDED: AI-DLC 成果物は V2 の space/intent モデルで管理し、旧 `aidlc-docs/` 構造を新規採用しない (2026-08-16)。
- DECIDED: 通常の作業 space は `default` とする (2026-08-16)。

## Forbidden

- NEVER `Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `UserSettings/` を設計上のソースまたはコミット対象として扱う。
- NEVER Unity の `.meta` と対応アセットの関係を無視して移動・削除する。

## Mandated

- ALWAYS シーンまたは Prefab の参照変更では、DI コンテナ境界と serialized reference の影響を確認する。
- ALWAYS 外部 endpoint と端末パスを扱うときは、タイムアウト、欠落、無効値、復旧を検討する。

## Corrections

<!-- プロジェクト固有の承認済み学習だけを追記する。 -->
