# Verification Test Results

Date: 2026-08-21

## Passed in Unity Editor 6000.2.6f2

| Check | Result | Evidence |
|---|---|---|
| C# compilation | PASS | Asset refresh 後に compilation 完了、Console の compiler error 0件 |
| Placement record validation/round trip | PASS | schema v1 JSON round trip と UUID/pose validation を実行 |
| Anchor-relative pose math | PASS | 任意 world pose から local pose を計算・再構成し、position error `1.490116E-08`、rotation error `0` |
| Configuration prefab wiring | PASS | Anchor button、Lock button、status の serialized reference と label を確認 |
| Configuration initialization | PASS | scene root active、初期 CanvasGroup non-interactable、Anchor 未設定時 Lock button disabled |
| Cube runtime wiring | PASS | `Rigidbody`、`Collider`、`Grabbable`、`GrabInteractable` が存在し、PointableElement と Rigidbody の injection を確認 |
| Unsupported provider path | PASS | Editor で Anchor 作成を実行し、成功を偽らず `Could not create or localize the Spatial Anchor.` を表示、Lock button disabled |
| QR/placement separation | PASS | `FoodViewModel` が `IFoodPlacementService.FoodTransform` を購読し、QR Transform は tutorial `QrPlateDetected` 経路だけに残ることを source review で確認 |
| Standalone separation | PASS | local selection が `NotifyFoodGuid` のみを呼び、QR pose/event を更新しないことを source review で確認 |

PlayMode の Console には Editor で Meta XR tracker が初期化されない既存エラーと controller visual の既存 AABB エラーが出るが、`FoodPlacement` 起因の例外は 0件だった。

## Quest 3 Device Acceptance — Pending

- 右 controller の A button で設定画面が開閉し、Grip で Cube を移動・回転できる。
- 十分な周辺視界で Anchor create/save が成功し、Anchor 自体は Cube 操作で移動しない。
- `Lock Food Position` 後、食品 world pose が Cube pose と一致する。
- アプリ再起動後、UUID load/localization と relative pose 復元により同じ物理位置へ表示される。
- QR を移動、回転、ロストしても食品 pose は変化せず、別 GUID の食品選択は継続する。
- 新 Anchor 作成失敗と relative pose 保存失敗で、旧 Ready 配置が失われない。
- 暗所、観測不足、permission/provider failure の案内と再試行を確認する。

Quest 実機結果がないため、NFR1/NFR3/NFR4 の本番環境合格および FR7 の再起動復元成功は未判定とする。
