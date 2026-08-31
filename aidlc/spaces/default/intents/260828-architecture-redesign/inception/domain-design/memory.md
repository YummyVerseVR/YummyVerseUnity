# Domain Design Memory

## Confirmed

- Target は Domain/Application core、role-specific port、Infrastructure adapter、Presentation coordinator、thin Unity View、composition root、lifecycle registry、verification gate に分ける。
- current directory 名は責務の保証ではなく、初期監査の migration map として扱う。
- `FoodDB` は初期時点で active graph 非到達の legacy candidate だが、削除完了とは記録していない。

## Decisions

- 新しい依存方向は View→application port、Application→Domain/port、Infrastructure→port implementation、composition root→concrete に限定する。
- Network と Standalone は port/adapter/failure/identity/lifecycle を分ける。
- View は plain C# collaborator へ責務を移し、partial/helper MonoBehaviour で規約を迂回しない。
- 承認済み architecture exception は現時点でない。

## Open Questions

- 実装後の最終 namespace/class/assembly 分割と既存型の移行順。
- `FoodInstaller` を統合または削除する際の asset reference 結果。
- 各 View の serialized reference を保ったまま collaborator を接続する具体的 wiring。
- 実装後の dependency scan、Unity load、Editor/device test evidence。
