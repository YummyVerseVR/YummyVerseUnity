# Partial Construction Implementation

## Result

- `UNIT-01`〜`UNIT-03`: `IMPLEMENTED-VERIFIED`
- Full intent Construction: `NOT-READY`
- 実装日: 2026-08-24

利用者が「現行 `aidlc` で決定済みの要件だけ」を実装するよう明示したため、`Q1`〜`Q11` と未公開の YummyService v2 HTTP contract に依存しない3 Unit だけを実装した。後続 Unit の仕様を推測していない。

## Delegation and Review

- `UNIT-01` の domain code と test の review・補強を、利用者指定どおり Luna/max subagent へ委譲した。
- Primary agent は共有差分を統合 review し、Editor 内テストで相対 operation が `file://` URI と誤解釈される診断不具合を検出した。
- 同 subagent へ局所修正を戻し、HTTP/HTTPS の absolute URI だけを URL sanitizer へ渡すよう修正した後、全テストを再実行した。

## Implemented Units

### UNIT-01: YummyService v2 domain contract foundation

- Reviewed snapshot の repository commit、OpenAPI version/checksum を domain constant に固定した。
- Order、5 stage、moderation、Food Analysis、artifact の既知 vocabulary を strict mapping し、unknown は成功状態へ変換しない。
- Network order identity を QR GUID と分離した opaque `GeneratedFoodItemId` とした。
- Selected immutable artifact を artifact ID、type、revision、SHA-256、verified の組として保持し、cache identity を同3 identity から構成した。
- `COMPLETED` order、5 stage completion gate、selected verified GLB のすべてを満たす場合だけ selectable とした。
- Download bytes の SHA-256 と metadata を比較する domain gate を追加した。
- v1 marker、未確認 contract revision、unknown enum、wrong/unverified artifact を fail closed にし、診断から URL userinfo/query/fragment と operation query/fragment を除去した。
- HTTP path、authentication、history、artifact lookup/download adapter は実装していない。

Traceability: `FR25`, `FR26`, `FR29`, `FR31`, `NFR10`, `NFR11`

### UNIT-02: Food identity runtime separation and v1 retirement

- QR GUID change を購読して旧 `/{guid}/model` を呼ぶ `FoodDownloader` と、その runtime binding を削除した。
- FoodDB の旧 test handler と QR→handler request を runtime から外し、QR ViewModel は designation 用 Transform 更新だけを行うようにした。
- `FoodContext` の load trigger を `OnMenuItemSelected` に変更し、既存 Standalone menu selection を game event 経由で local loader へ接続した。
- `NotifyFoodGuid` を QR detection interface/service から削除し、Standalone selection が QR state を変更しないようにした。
- v2 transport 未公開中は factory が Network loader を生成せず、connection test も outbound request を送らず `ServiceUnavailable` で fail closed にした。
- Legacy endpoint の既定値を空にし、設定値は HTTPS だけを受け入れるようにした。設定しても現時点では transport request を行わない。
- Local catalog ID と file 欠落、破損、未対応形式を item-level failure として返し、キャンセルだけは成功扱いにせず再送出するようにした。

Traceability: `FR13`, `FR16`, `FR25`, `FR34`, `FR35`, `NFR7`, `NFR11`

### UNIT-03: Food consumption state foundation

- 一つの Food Instance に属する portion state を View/collider/effect から独立した pure state として追加した。
- 有効 action ごとに一段階だけ残量を減らし、0未満にせず、remaining fraction を単調減少させる。
- 最終 action だけが `DishCleared` 相当結果を返し、完食後の重複 action は false となる。
- AABB、scoop detector、visual/collider scale、crumb/disappear、game event wiring は未実装である。

Traceability: `FR21`, `FR22`, `FR23`, `FR24`, `NFR4`, `NFR9`

## Explicitly Not Implemented

- `UNIT-04`: Unified catalog / Virtual Menu
- `UNIT-05`: Selected model / artifact delivery
- `UNIT-06`: QR anchor designation integration
- `UNIT-07`: AABB、scoop、visual/effect integration
- `UNIT-08`: Physical Menu Viewer
- YummyService v2 production HTTP path/auth/history/status/metadata/download
- Scene wiring の追加、Quest/PCVR build、実機検証、deployment

これらは `Q1`〜`Q11` または normative v2 transport contract に依存するため、本実装を根拠に完成扱いにしない。

## Changed Runtime Boundary

- New: `Assets/YummyVerse/Scripts/Model/YummyServiceV2/YummyServiceV2Domain.cs`
- New: `Assets/YummyVerse/Scripts/Model/FoodConsumptionState.cs`
- Removed: `Assets/YummyVerse/Scripts/Model/FoodDownloader.cs`
- Removed: `Assets/FoodDB/Scripts/Model/TestFoodDBHandler.cs`
- Updated: Food selection、QR detection、Standalone loading、endpoint/connection fail-closed、DI binding、config UI copy
- Tests: `Assets/YummyVerse/Editor/Tests/`

対応する新規 asset/script には `.meta` を追加し、削除した script と `.meta` は対で削除した。

## Working Tree Isolation Note

In-editor verification 中、active `Assets/YummyVerse/Scene/Restaurant.unity` の dirty UI layout override が disk へ保存された。差分は `YummyTutorialUI.prefab` instance の RectTransform override で、READY Unit の実装として作成・review・合格判定していない。利用者の Editor 作業を破棄しないため revert せず保持した。
