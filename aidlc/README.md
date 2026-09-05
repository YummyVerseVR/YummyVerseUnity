# AI-DLC Workflow V2 ドキュメントワークスペース

このディレクトリは、YummyVerseUnity の AI-Driven Development Life Cycle（AI-DLC）V2 成果物を管理する。構造は AWS Labs `aidlc-workflows` の `v2` ブランチ（確認基準: v2.6.2）に合わせている。

## このリポジトリでの配置

```text
aidlc/
└── spaces/
    └── default/
        ├── memory/       # 組織・チーム・プロジェクト・フェーズ別の運用ルール
        ├── knowledge/    # intent をまたいで再利用するドメイン知識
        ├── codekb/       # リポジトリ単位のリバースエンジニアリング成果
        └── intents/      # 1件の変更要求につき1つのライフサイクル記録
```

通常は `default` space を使う。別チームでルールや知識を分離する必要が生じた場合だけ、新しい space を追加する。

## 新しい intent

正式な AI-DLC V2 ランタイムを導入している環境では、intent はランタイムに作成させる。ドキュメントだけを手動運用する場合は、次の形式で作成する。

```text
aidlc/spaces/default/intents/<YYMMDD>-<short-kebab-label>/
├── aidlc-state.md
├── audit/
├── verification/
├── initialization/
├── ideation/
├── inception/
├── construction/
└── operation/
```

各 stage のディレクトリは実行時に必要なものだけ作成する。空の全 stage を先に作らない。Construction のコード本体は `aidlc/` ではなく通常の Unity プロジェクト配下へ置く。

## 成果物の流れ

1. Ideation で intent、対象範囲、価値を確定する。
2. Inception で要件、ユーザーストーリー、ドメイン設計、Unit、Delivery Plan を確定する。
3. Construction で Unit ごとに設計・実装し、最後に全体のビルドとテストを行う。
4. Operation で配布、現地確認、監視、障害対応、フィードバックを記録する。
5. 各 phase 境界で `verification/phase-check-<phase>.md` にトレーサビリティ確認を残す。

## 運用ルール

- `memory/org.md` → `team.md` → `project.md` → `phases/<phase>.md` の順で、狭いルールを追加する。上位ルールを暗黙に上書きしない。
- 人がレビューする文章は日本語で書く。ファイル名、ID、`[Answer]:`、`READY`、`NOT-READY` など機械処理されるトークンは変更しない。
- 要件 ID（`FR1`, `NFR1`）、ストーリー ID、ADR ID、Unit ID は一度付与したら変更しない。
- 判断、承認、却下、例外は audit に残す。推測は事実として扱わず、仮定または未解決事項として明示する。
- `aidlc-state.md`、audit、成果物、検証結果は Git 管理する。`.gitignore` に定義した個人カーソルと実行時ファイルはコミットしない。
- 現行の製品要件と実装判断は `aidlc` 内で自己完結させる。外部または削除予定の文書へのリンクだけで要件を表現しない。

## 現行の製品要件

- Tutorial、仮想/物理メニュー、QR/anchor、画像 preview、食事 action の統合要件は `spaces/default/intents/260824-guided-food-experience/` を参照する。
- コアロジック、依存方向、薄い View、DI、lifecycle、未使用コード判定、review gate の恒久規約は `spaces/default/knowledge/aidlc-shared/architecture-and-code-quality.md` を参照する。今回の適用計画・初期監査は `spaces/default/intents/260828-architecture-redesign/` に記録する。
- Tutorial の共有実装・運用知識は `spaces/default/knowledge/aidlc-shared/tutorial-system.md` を参照する。
- YummyService v2 の契約 snapshot と API policy は `spaces/default/knowledge/aidlc-shared/yummy-service-v2-api.md` を参照する。v1 API は廃止済みで、YummyVerseUnity から今後一切利用しない。
- Unity Device の v2 endpoint/schema（history、status、selected artifact、Hardware Payload/ACK、認証、残課題）は `spaces/default/knowledge/aidlc-shared/yummy-service-v2-unity-api.md` を参照する。
- `spaces/default/intents/260821-spatial-anchor-food-placement/` は Spatial Anchor 実装時の履歴である。その中の QR GUID による食品選択方針は 2026-08-24 intent により superseded されている。
- 咀嚼計シリアル通信プロトコル (`YummyVerse_Serial_Protocol_v1.1.md`, `YV-SERIAL-001`) の要点と Unity 側実装境界・回帰観点は `spaces/default/knowledge/aidlc-shared/chewing-sensor-serial-protocol.md` を参照する。v1.1 で導入されたキャリブレーションのフェーズ分割対応の要件・設計判断は `spaces/default/intents/260904-chewing-calibration-phase-split/` に記録する。

## 公式ランタイムとの境界

ここで整備したのは V2 互換のドキュメントワークスペースであり、`.codex/` の hooks・tools・agents を含む公式ランタイム本体ではない。公式ランタイムを導入する場合は、同じバージョンの Codex distribution を別途導入し、既存の `aidlc/spaces/default/memory/` をレビューしてから統合する。

## 参照基準

- [AWS Labs aidlc-workflows `v2` branch](https://github.com/awslabs/aidlc-workflows/tree/v2)
- [Spaces and Intents](https://awslabs.github.io/aidlc-workflows/guide/03-spaces-and-intents/)
- [Artifacts Reference](https://awslabs.github.io/aidlc-workflows/guide/14-artifacts-reference/)

最終確認日: 2026-08-16
