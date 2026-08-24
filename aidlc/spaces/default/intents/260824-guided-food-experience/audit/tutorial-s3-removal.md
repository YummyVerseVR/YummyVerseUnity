# Audit: Tutorial S3 Removal

## 2026-08-24

- Input: 利用者はチュートリアル中の「目の前の紙皿を見つめてください」を流れから削除するよう要求した。
- Decision: Main TutorialSequence から S3 TaskStep 全体を外し、表示だけを空文字にする実装は採用しない。
- Decision: デフォルト asset の再生成でも S3 が復活しないよう `TutorialAssetBuilder` の step/condition 生成と sequence 組立てから S3 を除外する。
- Boundary: Anchor designation の製品責務 (`FR16`/`FR17`) は削除しない。ただし guided tutorial の案内、QR detection wait、hint/rescue 対象にはしない。
- Preservation: 旧 `Step_S3_LookAtPlate.asset`、`Cond_QrPlateDetected.asset`、localization entry は履歴・参照安定性のため削除せず、active sequence からだけ切り離す。
