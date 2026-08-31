# Delivery Planning Memory

## Confirmed

- 実装単位を `UNIT-AR-01`〜`UNIT-AR-08` とし、監査→core→port/adapter→View→DI→削除→検証の順にした。
- asset/GUID 変更と unused deletion は依存移行・証拠確認の後段に置いた。
- C# compile、EditMode、contract、Unity load、Quest、PCVR/Editor を別 gate とした。

## Decisions

- `UNIT-AR-01` は active root/到達性の再監査を担い、初期監査をそのまま最終証拠にしない。
- `UNIT-AR-07` の削除は class reference、script GUID、active asset graph、tests/editor tooling の証拠必須。
- rollback は feature/unit 単位で行い、旧 v1 route、static singleton、View I/O を復活させない。

## Open Questions

- 実装 agent が採用する最終 unit 境界、namespace、assembly と compatibility seam。
- `FoodInstaller` の最終扱いと Prefab/Scene の wiring。
- どの legacy code が Editor tooling root として保持されるか。
- Quest/PCVR の検証環境、再現手順、結果。
