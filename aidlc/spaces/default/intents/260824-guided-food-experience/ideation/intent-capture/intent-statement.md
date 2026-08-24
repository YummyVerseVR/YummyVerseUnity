# Intent Statement

## Problem

YummyVerse のチュートリアル仕様は `docs/` に分散し、既存の `aidlc` は Spatial Anchor 配置だけを扱っている。このまま `docs` を削除すると、スタートから自由体験までの進行、データ駆動のチュートリアル構造、中断・救済、食品選択、プレビュー、外部ビューアー、すくい判定、食事演出の要件が失われる。また、従来の QR 読み込みをモデル生成・表示の起点にするフローは待ち時間を生み、2026-08-24 の製品方針では QR の責務を出現場所の指定だけへ縮小する必要がある。

## Desired Outcome

- VR 空間のスタートボタンから、説明を見ながら実際に操作できるチュートリアルを開始する。
- 初回はリンゴなどのオーソドックスな食品ですくう・食べる操作を体験し、その後に利用者が選んだ生成食品を提供する。
- チュートリアルは ScriptableObject によるデータ駆動のステップ列とし、ゲーム機能と一方向のイベント/コマンド境界で接続する。
- QR は食品の生成・選択に使わず、モデルの出現場所となるアンカーの指定にだけ使う。
- 生成済み食品を VR 内の履歴メニューから選び、モデル候補の一覧には軽量な画像プレビューを先行表示する。
- Tutorial 完了後の一つの食品選択 UI に、YummyService v2 由来の生成食品と端末保存済み Standalone 食品を並べ、どちらも選択・表示できる。
- iPad 等から生成済み食品を閲覧できる物理版メニューを提供する。
- Network mode は YummyService v2 の order/artifact contract を使用し、Quest と物理版メニューが同じ生成履歴・immutable artifact revision を参照する。
- 生成モデルへ簡易 AABB を付与してスプーンとの相互作用を検知し、食べるたびに縮小し、食べカスを出し、最後に消滅させる。
- `docs` が削除されても、本 intent と space-level knowledge だけで要求、制約、受け入れ条件、未解決事項を追跡できる。

## Primary Actors

- 来場者: チュートリアルを受け、生成食品を選び、すくって食べる体験を行う。
- 展示運営者: アンカー、メニュー、セッション、エラー復旧を管理する。
- 外部閲覧者: iPad 等の物理版メニューで生成済み食品を見る。
- 開発者: Unity/Quest、モデル配信、画像プレビュー、外部ビューアーの境界を保守する。

## Scope

- Attract、Tutorial、FreePlay、Outro を通した一連の展示セッション。
- データ駆動チュートリアルのステップ、条件、Presenter、イベント、コマンド、中断、救済、分析、デバッグ。
- 固定された前菜から利用者が選択した生成食品へ移行する体験フロー。
- QR のアンカー指定専用化、VR 内の生成履歴メニュー、画像プレビューの先行読込。
- API 非依存で端末内 model を利用する Standalone Mode と、Network/Standalone を統合する post-tutorial selection UI。
- iPad 等向け物理版メニューの閲覧要件。
- YummyService v2 の order/stage state、artifact identity/integrity、必要 consumer API capability と transport readiness gate。
- 生成モデルの簡易 AABB、すくいリアクション、任意の haptic、段階的縮小、食べカス、完食時消滅。
- 既存 `docs` から要件を移管し、`aidlc` を規範的な要件ソースにすること。

## Out of Scope

- 複雑な食べ物断面のリアルタイム生成、咀嚼・流体・破断の物理シミュレーション。
- iPad ビューアーの配布方式、認証方式、表示を画像だけにするか 3D まで含めるかの未確定事項を、この intent で無断に決定すること。
- 「即座」の数値目標、キャッシュ容量、履歴保持件数を根拠なく固定すること。
- 既存コードが要件を満たすと未検証のまま宣言すること。
- v2 OpenAPI に定義されていない endpoint/auth/download contract を推測し、v1 route を v2 として暫定利用すること。

## Success Criteria

1. `docs/` を参照せず、チュートリアル、メニュー/ロード、QR、外部ビューアー、食事アクションの全要求を恒久 ID で説明できる。
2. スタートから前菜体験、生成食品選択、提供、完食、Attract 復帰までのフローと中断/救済経路が定義されている。
3. QR が食品 identity source ではなく anchor designation source であることが明記され、旧 intent との優先関係が明確である。
4. AABB、すくい、縮小、食べカス、消滅について検証可能な受け入れ条件がある。
5. 未指定の性能値・外部ビューアー方式・AABB 算出詳細・haptic 必須度・既存 Spatial Anchor との統合方式・v2 transport/auth/artifact operation が未解決事項として残されている。
6. YummyService v2 の確定 domain vocabulary と未定義 transport contract を区別し、実装に必要な API capability と blocker を説明できる。
