# Partial Construction Test Results

## Environment

- Date: 2026-08-24
- Unity Editor: `6000.2.6f2`
- Editor state: project を既存 Unity Editor instance で refresh。別 batchmode instance は起動していない。
- Scope: `UNIT-01`〜`UNIT-03`

## Automated and Static Results

| Check | Result | Evidence |
|---|---|---|
| Unity script validation | `PASS` | READY Unit の runtime/test scripts で diagnostics 0 |
| C# project build | `PASS-WITH-EXISTING-WARNINGS` | `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: 0 errors、24 warnings。warnings は package/reference conflict と対象外の既存 deprecated API。変更対象の compiler warning は確認されなかった |
| Editor in-process NUnit methods | `PASS` | 20 passed、0 failed。4 test fixture の `[Test]` method を既存 Editor process 内で実行 |
| Prefab load | `PASS` | `YummyConfigUI.prefab` load 成功、missing script 0 |
| Standalone catalog validation | `PASS` | `LocalFoodSO.asset` の4 item が non-empty valid GUID を返した |
| Diff whitespace check | `PASS` | `git diff --check` に error なし。line-ending conversion warning は既存 Git 属性/working copy 差分として分離 |

## Test Coverage

### UNIT-01

- Opaque generated food/order identity
- 全既知 OrderState、StageState、StageType、ArtifactType、moderation/Food Analysis vocabulary
- Unknown enum fail-closed
- Exact wire vocabulary round trip
- Immutable artifact identity、revision、SHA-256 validation
- Download byte SHA-256 match/mismatch
- Completed order + selected verified GLB + five-stage selectable gate
- Example Retrieval だけの allowed warning
- Missing stage/artifact、wrong/unverified artifact rejection
- Reviewed contract revision acceptance、unknown revision rejection
- v1 URL/operation rejection と credential-safe diagnostic
- Review 中の Image-to-3D independent branch 表現
- Constructor input dictionary の snapshot isolation

### UNIT-02

- Menu selection が選択した Standalone GUID だけを load trigger にする
- Empty menu identity は load を開始しない
- QR detection は food load を開始しない
- Endpoint default は空、HTTP endpoint は拒否、HTTPS だけを設定値として受理
- v2 transport 未公開だった 2026-08-24 時点の connection test は request を送らず `ServiceUnavailable`。2026-08-30 の `ru322/main` route 公開後も、local Unity adapter は未移行である
- Runtime scan で `FoodDownloader`、`TestFoodDBHandler` binding、`NotifyFoodGuid`、QR→DB request、旧 server host、`UnityWebRequest` を READY Unit 対象から検出しなかった
- `/v1` 文字列は local negative rejection fixture/guard に限定された。`YummyVerse.FoodPlacement.v1` は API route ではなく既存 PlayerPrefs schema key である
- `OnChangeGUID` の残存購読は config diagnostics だけで、food load には接続されていない

### UNIT-03

- Non-positive portion count rejection
- 一 action で一段階だけ減少
- Remaining fraction の単調減少
- 0下限
- Final action の DishCleared one-shot と完食後の重複抑止

## Not Executed

- Unity Test Runner callback による標準 XML/report 出力
- PlayMode の Tutorial→FreePlay、session reset、actual local GLB parse/instantiate
- Quest 3 / PCVR / iPad 実機
- Spatial Anchor / QR designation integration
- AABB、scoop、scale、crumb、disappear、DishCleared event wiring
- Network request trace と YummyService v2 API integration
- Performance、10 session endurance、selection-to-visible latency

## 2026-08-30 API Contract Refresh

- External source snapshot: YummyService `ru322/main@97c9ed75980ec398fe75159bd4e011b489112433`。
- OpenAPI: 104 paths、124 schemas。Unity Device の history/status/artifact/payload/ACK route と `deviceBearerAuth` を確認した。
- Unity-specific schema summary and remaining gaps: `knowledge/aidlc-shared/yummy-service-v2-unity-api.md`。
- この refresh は external contract の読取り記録であり、Unity runtime の API migration、deployment host/TLS、実機通信を実行した結果ではない。

Active `Restaurant.unity` scene は検証終了時も dirty であり、保存された `YummyTutorialUI` layout override は本 Unit の scene wiring 検証対象から除外した。

未実施項目は `UNIT-04`〜`UNIT-08`、`Q1`〜`Q11`、実機/transport contract に依存する。今回の PASS をそれらの合格として扱わない。

## Result

- `UNIT-01`〜`UNIT-03`: `PASS`
- Partial delivery: `IMPLEMENTED-VERIFIED`
- Full intent Construction: `NOT-READY`
