# Delivery Plan

## Plan Status

`READY-FOR-IMPLEMENTATION`。これは再設計を実装するための計画であり、コード変更、削除完了、テスト成功を表さない。初期監査の事実は `audit/codex-redesign.md` に固定し、実装 agent が各 gate の証拠を追加した後に Construction/Verification を更新する。

## Delivery Principles

- active root を起点に feature 単位で移行し、単なる namespace/file move を再設計とみなさない。
- Domain/Application を先に pure seam 化し、adapter、Presentation、composition root をその契約に合わせる。
- serialized asset の rename/move/delete は最後に限定し、GUID/Prefab/Scene/UnityEvent/ScriptableObject を同時に検証する。
- unused code は依存 graph の証拠が揃った単位だけ削除する。Scene/Prefab attachment だけでは残置・削除を決めない。
- 既存 product decision と VR/MR の挙動を保ち、未定義の YummyService v2 transport や新機能をこの intent で推測しない。

## Units and Sequence

| Unit | 内容 | 主な成果 | 前提 | Gate |
| --- | --- | --- | --- | --- |
| `UNIT-AR-01` | active root/依存 graph の再監査 | root、DI、callback、UnityEvent、ScriptableObject、Editor test の evidence | 初期監査 | graph coverage、削除候補表 |
| `UNIT-AR-02` | Domain contracts/value objects と core state/policy の抽出 | Unity 非依存の Domain/Application seam | `UNIT-AR-01` | Domain/Application concrete leakage scan、EditMode tests |
| `UNIT-AR-03` | Application use case と role-specific port の再設計 | session/catalog/placement/eating/settings の command/state/port | `UNIT-AR-02` | dependency gate、source boundary、unit tests |
| `UNIT-AR-04` | Infrastructure adapter と DTO mapper の整理 | Network/Standalone/anchor/storage/input/glTF adapter | `UNIT-AR-03` | mapping/failure/cancel contract tests、v1 route scan |
| `UNIT-AR-05` | Presentation coordinator と View の薄型化 | ViewModel/Presenter、read-only state、command forwarding、薄い MonoBehaviour | `UNIT-AR-03`、`UNIT-AR-04` | View checklist、subscription owner、Prefab reference review |
| `UNIT-AR-06` | composition root/feature registration の整理 | Installer delegation、unique binding、NonLazy/IInitializable rationale | `UNIT-AR-03`〜`UNIT-AR-05` | container validation、空 Installer/multi-bind scan |
| `UNIT-AR-07` | unused code と legacy の削除 | evidence-backed deletion/detach と graph 更新 | `UNIT-AR-01`〜`UNIT-AR-06` | class/GUID/graph/tests evidence、compile、Unity load |
| `UNIT-AR-08` | 全体 verification と文書更新 | platform別 test result、traceability、construction close 判定 | 全 units | Editor/Unity/Quest/PCVR を別結果で確認 |

同一 Unit 内でも code change と asset change を分け、変更の根拠と rollback を残す。削除候補は `UNIT-AR-07` より前に一覧化できるが、削除の実行は参照移行後に行う。

## Feature Order

1. 共通 Domain value object/result と lifecycle contract。
2. Session/reset と catalog/selection（Network/Standalone source を分離）。
3. Placement/Spatial Anchor/QR designation と settings command。
4. Food model loading/eating interaction と event/command boundary。
5. Tutorial/Presenter、Food menu、Placement cube、Food、Config UI の View thinening。
6. Installer feature registration、Prefab/Scene wiring。
7. 根拠が揃った legacy/unused の削除。
8. compile、EditMode/contract、Unity load、Quest、PCVR/Editor の検証。

## Deletion Gate

削除対象ごとに次を満たすまで削除を完了扱いにしない。

- class/type/method reference を scan 済み。
- script GUID と対応 `.meta` を確認済み。
- active Scene/Prefab/ScriptableObject/UnityEvent graph を確認済み。
- DI bind、`NonLazy`、`IInitializable`、factory、Unity callback を確認済み。
- Editor tests/editor tooling の利用と保持/削除理由を確認済み。
- 削除後に compile、missing script/GUID、container validation、必要な Unity load を実行済み。
- 上記の evidence が `audit/` または対応 decision に残っている。

## Verification Gate

| Gate | 実行内容 | 成功の意味 | 未実行の扱い |
| --- | --- | --- | --- |
| `G-01` | C# compile | 参照/型がコンパイル可能 | `NOT-RUN`。asset/runtime success ではない |
| `G-02` | Domain/Application EditMode unit | core/use case の policy が決定的 | test 未追加は理由を記録 |
| `G-03` | adapter contract | mapping/failure/cancel/integrity | production API integration と分離 |
| `G-04` | Unity Scene/Prefab load | serialized reference/DI/scene wiring | compile のみでは合格不可 |
| `G-05` | Quest/Android/OpenXR | Quest 固有 input/XR/anchor/UI | 未接続環境は `NOT-RUN` |
| `G-06` | PCVR/Editor | PCVR と Editor の挙動 | Quest 結果から推測しない |

## Risk and Rollback

- Core 抽出で既存型の参照が壊れた場合は、source-specific adapter/compatibility seam を unit 単位で戻す。旧逆依存を恒久復活させない。
- View thinening で serialized reference が壊れた場合は、Prefab/Scene の GUID 差分を確認し、UI behavior を保つ最小 wiring へ戻す。UI generation/I/O を View に戻さない。
- Installer 整理で起動順や NonLazy が変わった場合は registration 単位で戻し、scope/activation rationale を修正する。
- legacy 削除で未確認の Editor tooling が壊れた場合は削除を停止し、必要な test root として明示する。v1/legacy runtime route の fallback を復活させない。
- asset rename/move/delete は `.meta` と参照の rollback 情報を持たない限り実行しない。

## Completion Criteria

全 FR/NFR の traceability、許可された依存方向、thin View、DI composition root、lifecycle owner、unused evidence、asset GUID gate、platform別 test result が揃ったときに限り intent-wide completion を判定する。実装 agent の報告だけではなく、該当する diff/test/load evidence を確認する。
