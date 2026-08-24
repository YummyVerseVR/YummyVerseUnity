# YummyVerseUnity Project Context

## Product

YummyVerse は、食感再現を目的とした展示型 VR/MR アプリケーションである。来場者はガイド付きチュートリアルでリンゴ等の基本食品をすくって食べる操作を学び、その後、一つの仮想メニューに統合された YummyService v2 の生成食品と端末保存済み Standalone 食品から選んで体験する。食品 identity はメニュー item から取得し、QR コードはモデルの出現場所となる anchor designation にだけ使用する。Network item は画像 preview を先行表示し、iPad 等の物理版メニューからも閲覧可能にする。Standalone は API 非依存の local source として継続する。

この段落は target product requirement を表す。現行 code には QR GUID を食品選択へ使う経路など未移行部分があり、`260824-guided-food-experience` の Construction 完了までは target と current implementation を同一視しない。

## Primary Actors

- 来場者: 食べ物を選択し、Spatial Anchor を基準に固定された位置で VR/MR 体験を行う。
- 展示運営者: コントローラーから設定画面を開き、Spatial Anchor、食べ物の表示位置、endpoint、Food Scale、Standalone Mode、接続・読込状態を管理する。
- 外部閲覧者: iPad 等の物理版メニューから生成済み食品を閲覧する。
- 開発者: Unity、Quest、PCVR、外部 Yummy Control Server 間の統合を保守する。

## Main Runtime Flow

1. VR 空間の Start から Tutorial を開始し、説明に沿って anchor designation と基本的な食事操作を行う。
2. QR recognition は出現 anchor の指定にだけ使い、食品の生成・選択・download key には使わない。
3. リンゴ等の前菜を指定 anchor へ表示し、AABB による scoop、段階縮小、crumb、消滅を体験する。
4. scene を変えず FreePlay へ移り、一つの食品選択 UI に v2 API 由来の生成食品と Standalone local 食品を一覧表示する。
5. 選択 item の source に応じ、Network は selected verified model、Standalone は local model を load し、指定 anchor へ表示する。
6. 同じ生成履歴を iPad 等の物理版メニューへ提示する。
7. 中断・完食後は session 一時状態を reset し、生成履歴と有効な展示 placement は保持する。

## Architectural Landmarks

- `Assets/YummyVerse/Scripts/Model/`: 状態、入力、Spatial Anchor、表示位置の永続化、QR、ネットワーク、食べ物、イベント、リセット。
- `Assets/YummyVerse/Scripts/ViewModel/`: UI/ゲーム表示の調停とチュートリアル進行。
- `Assets/YummyVerse/Scripts/View/`: Unity `MonoBehaviour` と表示・端末境界。
- `Assets/YummyVerse/Scripts/*/DI/`: Extenject Installer。
- `Assets/YummyVerse/Data/Tutorial/`: ScriptableObject ベースの条件、step、sequence、localized data。
- `Assets/YummyVerse/Scripts/Tests/`: 現在確認できる Unity テスト領域。
- `aidlc/spaces/default/knowledge/aidlc-shared/tutorial-system.md`: チュートリアルの実装境界、Editor/scene baseline、編集・reset・debug の共有知識。

## External and Device Boundaries

- Meta Quest 3 / OpenXR / Meta XR SDK
- Meta XR Spatial Anchor と永続化された Anchor UUID
- XR Interaction Toolkit によるコントローラー操作と設定用 Cube の grab interaction
- 出現 anchor designation 用の QR trackable と MRUK（食品 identity には不使用）
- Legacy current-code boundary: Yummy Control Server endpoint。v2 migration 後の target runtime/fallback では使用しない。
- YummyService v2 order/artifact API。現行 target は v2 のみで、v1 は廃止済み・利用禁止。
- HTTP による preview image/selected immutable GLB download と SHA-256 integrity verification
- Android `Application.persistentDataPath` 配下の Standalone food files
- Unity Localization、Addressables、glTFast
- 生成食品 catalog、preview image cache、選択 model data cache/source
- iPad 等の物理版メニュー viewer と、その未確定の transport/auth boundary

## Normative Documentation

- 現行の統合要件: `aidlc/spaces/default/intents/260824-guided-food-experience/inception/requirements-analysis/requirements.md`。
- 移管元との coverage: 同 intent の `source-migration-map.md`。
- Tutorial の共有実装/運用知識: `aidlc/spaces/default/knowledge/aidlc-shared/tutorial-system.md`。
- YummyService v2 の契約 snapshot、必要 API capability、v1 廃止方針: `aidlc/spaces/default/knowledge/aidlc-shared/yummy-service-v2-api.md`。
- Spatial Anchor 実装履歴: `aidlc/spaces/default/intents/260821-spatial-anchor-food-placement/`。ただし QR GUID 継続方針は新 intent により superseded。
- `docs/` は移管 provenance として残っていても規範的な参照先にしない。削除されても要件判断に影響しない。

API version policy: YummyVerseUnity が使用するのは YummyService v2 だけである。v1 API は廃止済みで、全 runtime/environment/fallback からの outbound 利用を恒久的に禁止する。

Standalone policy: Standalone Mode は今後も維持する。Standalone は API を使わない端末内 source であり、v1 fallback ではない。Network/API availability に関係なく有効な local item を一覧・選択・表示できる。

## Known Gaps

- `README.md` の推奨 Unity 版と `ProjectSettings/ProjectVersion.txt` の実版が一致していない。
- 自動テストの網羅性、CI、Quest/PCVR の再現可能な実機テスト手順は、今後の intent で確認・補強が必要。
- YummyService v2 の domain contract は確認済みだが、production HTTP paths/auth/artifact operation は規範 OpenAPI に未定義。
- 現行 implementation の QR GUID food selection は target requirement と一致せず、migration が必要。
- 現行 `FoodDownloader`/`FoodContext`/`IFoodFetchable` は旧 GUID model download で、YummyService v2 order/artifact contract と不一致。
- YummyService v2 normative OpenAPI は現時点で `2.0.0-draft`、`paths: {}`、placeholder server URL で、auth/artifact lookup/download が deferred。Production HTTP integration は未準備。
- Model selection-to-visible SLA、physical viewer、AABB、haptic、QR/Anchor、v2 transport/auth/history/artifact visibility/download は `260824-guided-food-experience` の `Q1`〜`Q11`。
