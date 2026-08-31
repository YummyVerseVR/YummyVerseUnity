# 初期監査: Architecture Redesign

## 監査目的

YummyVerseUnity の active runtime から実際に到達するコードと、現行の責務・依存方向を確認し、再設計と unused code 判定の起点を固定する。ここにない推測は事実として扱わない。実装 agent の変更後に再走査する前提であり、この文書は redesign 完了報告ではない。

## Scope と root

| 観点 | 初期監査で確認した事実 | 根拠 |
| --- | --- | --- |
| Build scene | enabled の build scene は `Assets/YummyVerse/Scene/Restaurant.unity` のみ | `ProjectSettings/EditorBuildSettings.asset` |
| 再帰 graph | active scene から再帰参照される app script は 34 個。DI から pure service が追加到達する | 初期 runtime/asset graph scan の引き継ぎ結果 |
| DI/lifecycle root | `RestaurantInstaller` に多数の binding があり、`NonLazy`/`IInitializable` と Unity callback を別の root として追う必要がある | `Assets/YummyVerse/Scripts/Model/DI/RestaurantInstaller.cs`、`Assets/YummyVerse/Scripts/ViewModel/DI/`、active graph |
| Editor test root | `Assets/YummyVerse/Editor/Tests/` の Editor tests を runtime とは別 root として保持する | 初期 test inventory |

## 観測された設計上の問題

| 対象 | 初期観測 | 再設計上の扱い |
| --- | --- | --- |
| View | `FoodSelectionMenuView.cs` 477 行、`FoodPlacementCubeView.cs` 269 行、`FoodView.cs` 219 行、`ConfigUIView.cs` 219 行 | UI 生成、表示、input forwarding、policy、lifecycle を分類し、plain C# collaborator/use case へ抽出する。単なる partial 化は不可 |
| Installer | `RestaurantInstaller` に多数 binding。`FoodInstaller` は空だが `FoodView.prefab` に付与されている | feature registration へ委譲し、空 Installer/scene component の意味を検証する |
| Model/Service | Model 配下に catalog、network/local loader、placement、input、eating、event/command、reset 等が混在する | Domain/Application と Infrastructure adapter、role-specific port へ分割する |
| Legacy | `Assets/FoodDB/Scripts` は active graph 非到達で、obsolete WWW 警告を発生させる | class reference、script GUID、asset graph、tests/editor tooling を確認してから削除候補を判定する |

## Baseline

- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`: **0 errors / 25 warnings**。これは再設計実装前の baseline であり、再設計後の成功を意味しない。
- Editor/Unity load、Quest/Android/OpenXR、PCVR の再設計後結果は未確認である。
- `FoodSelectionMenuView` 等の行数は責務混在のシグナルであり、行数だけを削除・分割の判定基準にはしない。

## 依存・到達性監査の手順

1. enabled build scene を列挙し、active scene を root として固定する。
2. Scene → Prefab → serialized component/UnityEvent → ScriptableObject/asset を再帰走査する。
3. code call/reference、DI bind/activation、`NonLazy`/`IInitializable`、Unity lifecycle callback を追加の到達経路として記録する。
4. Editor tests と editor tooling は別 root として、runtime 非到達でも必要かを確認する。
5. 削除候補ごとに class reference、script GUID、参照 asset GUID、active asset graph、tests/editor tooling、コンパイル/Unity load 影響を表にする。
6. 実装後に同じ走査を再実行し、missing script、未解決 GUID、DI activation failure、意図しない legacy route がないことを確認する。

## 初期監査でまだ確定していないこと

- 34 個の scene 到達 script と DI 追加到達 service の最終分類（Domain/Application/Infrastructure/Presentation）。
- `FoodDB` の全 class/asset/test/editor tooling に、active root 以外の必要な利用経路がないこと。
- 大きな View ごとの抽出単位と serialized field の移行方法。
- `FoodInstaller` を削除、統合、または feature registration へ置換する際の Prefab/Scene GUID 影響。
- 実装 agent の変更後の compile、EditMode、contract、Unity load、Quest、PCVR 結果。

## 監査状態

`INITIAL-FACTS-RECORDED`。初期事実と設計仮説を分離して記録した。実装完了、unused code の削除完了、テスト成功は未判定である。
