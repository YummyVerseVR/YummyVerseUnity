# Intent Statement

## Problem

YummyVerseUnity は機能追加に伴って Model、ViewModel、View、DI、外部 SDK 境界の責務が混ざり、View の MonoBehaviour が UI 生成・状態遷移・I/O・購読管理を抱えやすくなっている。汎用 interface と複数 source の binding、空 Installer、legacy code の残存も、変更範囲と未使用判定を不明瞭にする。このままでは設定 UI、catalog、Spatial Anchor、食事 action、Tutorial の変更が互いの lifecycle と依存関係を壊す。

## Desired Outcome

- Domain の contracts/value objects と Application の use case を plain C# としてテスト可能にする。
- Network、Standalone、filesystem、PlayerPrefs、Meta XR、glTF、input などの外部境界を role-specific port と adapter に隔離する。
- MonoBehaviour View を serialized references、Unity lifecycle、render/input event forwarding、tick forwarding に限定する。
- Installer を composition root として feature registration に委譲し、具象 `new`/bind と lifetime ownership を明確にする。
- active runtime roots と Editor test roots に基づき、呼び出されないコードを証拠付きで削除する。
- 既存の製品要件と VR/MR の表示・操作挙動を保ったまま、再設計後の依存方向とテスト境界を review できるようにする。

## Scope

- `Assets/YummyVerse/Scene/Restaurant.unity` とその再帰 Prefab/asset graph。
- DI `NonLazy`/`IInitializable`、Unity lifecycle callbacks、Editor tests を含む到達性監査。
- Core/Domain、Application/use case、Infrastructure adapter、Presentation/View、composition root の責務再配置。
- catalog/selection、placement、food interaction、session/reset、Tutorial、settings UI など、既存 active feature の依存境界整理。
- unused code の証拠収集、削除候補判定、serialized asset/GUID 検証、テスト計画。
- `aidlc` の恒久設計規約と今回の実装計画の記録。

## Out of Scope

- 製品要件の変更、YummyService v2 contract の推測、QR の food identity 復活。
- 新しい runtime feature、UI visual redesign、未定義の外部 API/auth/transport の追加。
- `Assets/`、`Packages/`、`ProjectSettings/`、コード、Prefab、Scene の documentation agent による直接編集。
- active root の証拠なしに legacy/sample code を一括削除すること。
- Quest/PCVR の未実施結果を成功として扱うこと。

## Stakeholders

- 開発者: core/use case を unit test し、変更時の依存方向を追跡する。
- 展示運営者: settings、placement、catalog、session reset を壊さず利用する。
- 来場者: Tutorial、food selection、食事 action を同一 session で体験する。
- 実装・レビュー agent: 明示された root、port、lifecycle、削除証拠、検証 gate に従う。

## Success Criteria

1. active runtime root と Editor test root が再現可能な手順と証拠で固定されている。
2. Domain/Application が Unity/外部 concrete を参照せず、許可された依存方向が traceability で確認できる。
3. View の責務と購読/lifecycle owner が明示され、UI 生成・I/O・policy・状態遷移が plain C# collaborator/use case にある。
4. Network/Standalone、transport DTO/domain、composition root/adapter の境界が role-specific port で確認できる。
5. unused code の削除候補に class reference、script GUID、active graph、tests/editor tooling の証拠があり、asset 変更は GUID/Unity load 検証を伴う。
6. EditMode、contract、Unity load、Quest、PCVR/Editor の実行結果が分離され、未実行を成功扱いしない。
7. 実装完了とテスト成功は、実際の証拠が追加された後にだけ intent state と verification へ反映される。
