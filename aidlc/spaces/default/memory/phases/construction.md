# Construction Phase Guardrails

## Code Completeness

- コンパイル不能な参照、未説明の placeholder、未処理のキャンセルや例外を残さない。
- 既存の Model/ViewModel/View と Interface の境界を尊重する。
- アセット変更には対応する `.meta` を含め、GUID 参照を維持する。

## Testing Standards

- 承認済み plan にテストまたは明示的な代替検証を含める。
- 自動テスト、Editor 確認、Quest 実機、PCVR 実機の結果を区別する。
- 実行していない検証を成功として記録しない。

## Safety

- Scene/Prefab/ProjectSettings の変更は範囲を限定し、関係のない serialized data を機械的に整形しない。
- endpoint、認証情報、端末固有パスをハードコードしない。

## Corrections
