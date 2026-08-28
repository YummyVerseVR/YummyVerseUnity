# Architecture Redesign Test Results

## Environment

- Date: 2026-08-28
- Unity Editor: `6000.2.6f2`（project を既存 Editor instance が開いており、`Temp/UnityLockfile` が存在する）
- Editor 自動化: Unity MCP endpoint は `ConnectionRefused`。batchmode instance も project lock により起動していない。
- Build 環境: `dotnet` 8.0.422（Unity 生成 `.csproj` に対する out-of-editor compile）
- Scope: `UNIT-AR-01`〜`UNIT-AR-07` のコード変更

## Gate Results

| Gate | 内容 | Result | Evidence |
| --- | --- | --- | --- |
| `G-01` | C# compile | `PASS` | `dotnet build Assembly-CSharp.csproj --no-restore -t:Rebuild`: 0 errors。`Assembly-CSharp-Editor.csproj`: 0 errors。`warning CS` は 0 件。残る 22/24 warnings はすべて `MSB3277` の package reference conflict で、baseline から存在する |
| `G-02` | Domain/Application EditMode unit | `NOT-RUN` | 14 fixture / 64 `[Test]` が `Assets/YummyVerse/Editor/Tests` に存在し compile は通るが、Editor が project を lock しており Test Runner を実行していない |
| `G-03` | adapter contract | `NOT-RUN` | `FoodCatalogServiceTests`、`FoodLoaderRouterTests`、`NetworkContractGateTests`、`PersistentFoodCatalogScannerTests` は追加済みだが未実行 |
| `G-04` | Unity Scene/Prefab load | `NOT-RUN` | 静的 GUID 検証のみ実施（下記）。Editor による実際の load、DI container validation、`NonLazy`/`IInitializable` activation は未確認 |
| `G-05` | Quest/Android/OpenXR | `NOT-RUN` | 実機未接続 |
| `G-06` | PCVR/Editor | `NOT-RUN` | 未実行。`G-05` から推測しない |

## Static Asset Verification

`G-04` の代替ではなく、その前提条件として実施した静的検査。

| Check | Result | Evidence |
| --- | --- | --- |
| active scene の script GUID 解決 | `PASS` | `Restaurant.unity` の `m_Script` GUID に未解決 0 件 |
| YummyVerse prefab の script GUID 解決 | `PASS` | `Assets/YummyVerse/Prefabs` 配下 prefab の `m_Script` GUID に未解決 0 件 |
| 削除 script の dangling 参照 | `PASS` | WIP commit で削除された 33 script GUID を `Assets`/`ProjectSettings` 全走査、被参照 0 件 |
| 削除 folder `.meta` の dangling 参照 | `PASS` | 削除した 12 folder GUID の被参照 0 件 |
| orphan `.meta` | `PASS` | `Assets/YummyVerse` と `Assets/FoodDB` に対応 asset のない `.meta` は 0 件 |
| `m_EditorClassIdentifier` の型解決 | `PASS-WITH-PREEXISTING-GAP` | `Assembly-CSharp` の 36 型は解決。`Food3DModel.View.QRreader` のみ未解決だが、本 intent 以前から欠落しており enabled build scene 非到達 |
| 依存方向 scan | `PASS` | `Scripts/Model` 配下（`YummyServiceV2` 除く）に `PlayerPrefs`/`UnityWebRequest`/`System.IO`/`OVR*`/`MRUK*`/`InputSystem`/`persistentDataPath` の concrete 参照は残っていない。`Model/DI` の composition root が concrete 型名を持つのは設計どおり |
| Infrastructure の逆依存 scan | `PASS` | 移設した 12 adapter は `Model` root の Application 型を 1 件も参照せず、`Model.Interface` と `Model.Struct` のみに依存する |

## Warning Delta

baseline の 25 warnings には `Assets/FoodDB/Scripts` 由来の obsolete `WWW` API warning が含まれていた。当該 script 削除後、`warning CS` は 0 件になった。

## Not Executed

- Unity Test Runner による EditMode/PlayMode 実行と XML report 出力
- Zenject container validation、`NonLazy`/`IInitializable` の activation 順序
- `Restaurant.unity` および `FoodView.prefab`、`YummyConfigUI.prefab`、`YummyTutorialUI.prefab` の Editor load
- Tutorial→FreePlay flow、session reset、Standalone 選択、YummyService v2 transport
- Spatial Anchor 作成/保存/localize、QR designation
- 設定 UI、virtual keyboard、controller interaction、VR 空間の表示順
- Quest 3 実機、PCVR 実機、性能・耐久

## Result

- `G-01`: `PASS`
- 静的 asset/依存検査: `PASS`
- `G-02`〜`G-06`: `NOT-RUN`
- Construction: `CODE-COMPLETE-PENDING-RUNTIME-VERIFICATION`
- Intent-wide completion: `NOT-READY`

`G-01` と静的検査の合格を、Scene load、DI activation、実機動作の合格として扱わない。次の作業は Unity Editor で Test Runner（EditMode）を実行し、`Restaurant.unity` を load して missing script と container validation を確認することである。
