# Domain Components

## Historical Scope Notice

この設計は 2026-08-21 の実装境界を記録する。`Existing Food Selection` にある QR GUID identity source は `260824-guided-food-experience` の Generated Food Catalog/Virtual Menu により superseded された。Spatial Anchor Placement Model、Persistence Store、Configuration、Food Placement Consumer の非競合責務は継続する。

## Design Goal

食べ物の identity source と placement source を分離する。QR/Standalone selection は `FoodContext` へ食べ物を供給し、Spatial Anchor placement は `FoodViewModel`/`FoodView` へ pose を供給する。両者が揃った場合だけ食べ物を確定位置へ表示する。

## Component Responsibilities

### Spatial Anchor Placement Model

- Spatial Anchor の作成、保存、UUID による読み込み、localization、置換を統括する。
- `Unconfigured`、`Loading`、`Editing`、`Saving`、`Ready`、`Error` の状態を公開する。
- Anchor が使用可能な場合だけ anchor Transform と確定済み anchor-relative pose を公開する。
- Anchor SDK の非同期処理を直列化し、成功・失敗・cancellation を状態へ反映する。

### Placement Persistence Store

- 次の一レコードを schema version 付きで保存・読み込み・削除する。
- JSON/PlayerPrefs 等の保存媒体はこの境界内に隠し、View/ViewModel から直接アクセスしない。
- UUID、position、rotation、確定状態を完全に検証できないレコードは無効として返す。

```text
SpatialAnchorPlacementRecord
├── schemaVersion: int
├── anchorUuid: string
├── foodLocalPosition: { x, y, z }
├── foodLocalRotation: { x, y, z, w }
└── isPlacementConfirmed: bool
```

### Placement Configuration ViewModel

- 既存の設定画面表示 action を購読し、画面の表示状態を切り替える。
- Anchor の作成/置換と配置確定を command として公開し、設定画面を開く操作を配置編集開始として扱う。
- 状態ラベル、error、各 button の interactable、設定用 Cube の表示/grab 可否を導出する。
- View へ Meta SDK の具象型を公開しない。

### Placement Configuration View

- 設定画面に Anchor 状態、操作 button、配置状態、最後の error を表示する。
- 設定用 Cube と controller grab component の Unity scene 参照を保持する。
- 設定画面が表示されて `Editing` または `Error` のときだけ Cube を表示して grab を許可し、それ以外では操作を無効にする。
- Cube は Spatial Anchor と別の world-space GameObject とし、確定時にだけ Anchor からの local pose を計算する。
- Anchor 作成前は Cube の world pose を Anchor 作成 pose とし、作成成功後も Anchor 自体は Cube の grab 対象にしない。

### Food Placement Consumer

- `FoodViewModel` は QR Transform ではなく Placement Model の ready pose を購読する。
- `FoodView` は食べ物用 root を anchor-relative pose へ配置し、QR trackable への毎フレーム追従を行わない。
- Food Scale の local scale と、glTF の既存の補正 rotation は placement rotation と別階層で適用し、保存 pose を破壊しない。

### Existing Food Selection

- `FoodContext` は通常モードの QR GUID および Standalone Mode の local selection を継続して扱う。
- `QRDetectionService` の GUID stream は selection source として残せるが、Transform stream は placement consumer へ接続しない。

## State Model

| State | Meaning | Allowed operator actions | Food pose usable |
|---|---|---|---|
| `Unconfigured` | 保存済み設定がない | Anchor 設定開始 | No |
| `Loading` | UUID から load/localization 中 | 待機のみ | No |
| `Editing` | Cube で Anchor または食品 pose を設定中 | Cube grab、Anchor 作成、確定 | 既存の確定配置がある再設定中だけ旧 pose を継続 |
| `Saving` | Anchor または食品 pose の保存中 | 待機のみ | 既存の確定配置がある再設定中だけ旧 pose を継続 |
| `Ready` | Anchor と relative pose が確定 | 再編集、Anchor 置換 | Yes |
| `Error` | 作成/保存/復元に失敗 | 再試行、再設定 | No。ただし Anchor 置換失敗時に旧 `Ready` 設定を保持できる場合は旧設定を継続 |

## Runtime Flows

### First-time Setup

1. Controller の設定 action で設定画面を開き、draft Cube を表示する。
2. 運営者が Cube を grab し、Spatial Anchor を作成したい world pose へ配置する。
3. 運営者が `Set Anchor` を実行し、Cube の現在 world pose に Meta Spatial Anchor を作成する。
4. Meta Spatial Anchor の端末への保存が成功したら、Anchor UUID を保持して `Editing` へ遷移する。
5. Anchor はその world pose に固定したまま、運営者が Cube だけを grab して食べ物の position/rotation を調整する。
6. 運営者が `Fix Food Position` を実行する。
7. Anchor UUID と Cube の anchor-relative pose を一レコードとして保存する。
8. Placement Model が `Ready` となり、Food Placement Consumer が Cube と同じ world pose を使用する。

### Startup Restore

1. Placement Persistence Store からレコードを検証して読み込む。
2. Anchor UUID で保存済み Spatial Anchor を load し、localization 完了を待つ。
3. Anchor Transform の子座標へ保存済み local pose を適用する。
4. `isPlacementConfirmed` が true の場合だけ `Ready` を公開する。
5. 食べ物モデルが既に取得済みなら表示し、未取得なら取得後に同じ pose へ表示する。

### Reconfiguration

1. 同じ Anchor 内の再編集では `Ready` から `Editing` へ戻し、既存 local pose から Cube を表示する。
2. 確定時に local pose を更新し、保存成功後に `Ready` へ戻る。
3. Anchor 置換では新 Anchor を先に作成・保存し、新 UUID と pose の保存が成功してから active setting を切り替える。
4. 旧 Anchor の erase は active setting の切り替え後に行い、失敗は stale-anchor cleanup として記録する。

## UI Contract

- 既存の endpoint、接続確認、Standalone Mode、Food Scale の UI を維持する。
- Anchor section に `Anchor status` と `Set / Update Spatial Anchor` を置く。この操作は draft Cube の現在 world pose を入力とし、失敗後の再試行にも使用する。
- Placement section に `Lock Food Position` と `Placement status` を置く。設定画面を開いた時点で配置編集を開始する。
- Anchor が使用可能でない場合は placement edit/fix を無効にする。
- 非同期処理中は重複する作成・保存・復元操作を無効にする。
- error は次の復旧操作が分かる文言とし、ログだけに閉じない。

## Failure Handling

| Failure | Required behavior |
|---|---|
| 保存レコードなし | `Unconfigured`。設定画面から新規作成できる |
| UUID/pose/schema 不正 | レコードを適用せず `Failed`。再設定を案内する |
| Anchor load 失敗 | world pose を推測せず `Failed`。retry/reconfigure を許可する |
| localization 未完了/失敗 | `Ready` を公開せず、処理中または失敗として表示する |
| Anchor save 失敗 | UUID を永続レコードへ commit しない |
| pose 保存失敗 | 新 pose を確定扱いにしない。可能なら旧 `Ready` 設定を維持する |
| 置換後の旧 Anchor erase 失敗 | 新設定を維持し、cleanup warning を記録する |
| Anchor provider 非対応 | 成功を表示せず、実機が必要であることを表示/記録する |

## Verification Boundaries

- EditMode: 保存レコード validation、状態遷移、QR identity/placement 分離。
- PlayMode: 設定画面 toggle、Cube の edit/fix 可否、Food pose の反映、session reset 後の保持。
- Quest 実機: Anchor create/save/load/localization、再起動後の復元、controller grab、QR 移動・ロスト時の pose 不変。
- PCVR/Editor: provider 非対応時の表示と復旧経路。Quest 実機成功の代替とは扱わない。
