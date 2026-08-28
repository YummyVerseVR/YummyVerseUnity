# Construction: Implementation Plan and Initial Findings

## Status

`IN-PROGRESS`。別の実装 agent がコード再設計を進めている。本稿は main design、implementation plan、事実確認できる初期監査だけを記録する。実装完了、unused code 削除完了、build/test 成功、Unity/Quest/PCVR 検証成功はまだ記録しない。

## Initial Findings (baseline)

- active build scene は `Assets/YummyVerse/Scene/Restaurant.unity` のみである。
- active scene から再帰参照される app script は 34 個で、DI から pure services が追加到達する。
- baseline `dotnet build Assembly-CSharp.csproj --no-restore --nologo` は 0 errors / 25 warnings である。これは redesign 後の結果ではない。
- `FoodSelectionMenuView` は 477 行、`FoodPlacementCubeView` は 269 行、`FoodView` は 219 行、`ConfigUIView` は 219 行である。
- `RestaurantInstaller` には多数の binding がある。
- `FoodInstaller` は空だが `FoodView.prefab` に付与されている。
- `Assets/FoodDB/Scripts` は初期監査上 active graph 非到達で、obsolete WWW 警告を発生させる legacy candidate である。

詳細な根拠と未確定事項は `audit/codex-redesign.md` にある。上記は初期 baseline であり、実装 agent の変更後に再確認する。

## Main Design

```text
Domain contracts/value objects
        -> Application use cases and role-specific ports
        -> Infrastructure adapters / Presentation coordinators
        -> thin Unity View and device/transport boundary
        -> composition root binds concrete implementations
```

- Domain/Application は Unity、View concrete、network/filesystem/PlayerPrefs、Meta XR、glTF、input concrete を知らない。
- Application は session/business decisions、状態遷移、catalog/selection、placement、eating、settings の use case を持つ。
- Network/Standalone、placement/anchor、QR designation、artifact/model、input の port は役割ごとに分ける。
- transport DTO/raw JSON/SDK type は adapter mapper 境界へ閉じ込める。
- View は serialized refs、lifecycle、render/input forwarding、tick forwarding だけを行い、UI generation/I/O/policy/state transition/subscription ownership を持たない。
- Installer は composition root として feature registration に委譲する。
- read-only state/property と command method、subscription/cancellation/disposal owner を feature ごとに定義する。

## Planned Implementation Sequence

1. active root、DI、callback、serialized reference、Editor test の graph を再監査する。
2. Domain contracts/value objects と Application use case の seam を作り、pure core を unit test 可能にする。
3. Network/Standalone、placement、anchor、input、model loading の role-specific port と adapter mapper を整理する。
4. session/reset、catalog/selection、placement/settings、food interaction/Tutorial の policy/state transition を use case/collaborator へ移す。
5. `FoodSelectionMenuView`、`FoodPlacementCubeView`、`FoodView`、`ConfigUIView` を含む View の責務を分離する。partial MonoBehaviour への移動は行わない。
6. Installer の feature registration、NonLazy/IInitializable、Prefab/Scene serialized wiring を検証する。
7. 到達性と GUID/asset graph の evidence が揃った unused/legacy candidate だけを削除する。
8. compile、EditMode unit、adapter contract、Unity load、Quest、PCVR/Editor を個別に実行して結果を追記する。

## Required Evidence Before Completion

- `traceability.json` の FR/NFR と実装 class/port/test の対応。
- dependency scan と architecture checklist。
- View responsibility table と subscription owner。
- DI container validation と Installer/NonLazy/IInitializable inventory。
- 削除候補ごとの class reference、script GUID、active graph、tests/editor tooling evidence。
- `.meta` GUID、Prefab/Scene/ScriptableObject/UnityEvent の参照検証。
- platform別の `PASS`/`FAIL`/`NOT-RUN` test result。

## Not Yet Claimed

- 再設計がコードに完全反映されたこと。
- `FoodDB` または他の candidate が安全に削除されたこと。
- baseline build が redesign 後も成功すること。
- Unity Editor load、Quest、PCVR の動作確認が成功したこと。
- 既存の settings UI、controller interaction、VR display ordering、Tutorial、Standalone、YummyService v2 の回帰が解消されたこと。
