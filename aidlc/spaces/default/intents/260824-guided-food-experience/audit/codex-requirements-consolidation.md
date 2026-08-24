# Audit: Requirements Consolidation

## 2026-08-24

- Input: `docs/tutorial-requirement.md`、`docs/tutorial-usage.md`、同日の利用者追加要求、`docs` 削除後の自己完結性要求。
- Decision: 既存の Spatial Anchor intent を拡張せず、来場者体験全体を扱う `260824-guided-food-experience` を新設する。
- Reason: チュートリアル、モデル履歴、外部ビューアー、食事アクションは旧 intent の配置変更より広く、QR の食品選択責務を上書きするため。
- Decision: 旧 intent は過去の実装判断として保持し、supersession notice を追加する。
- Decision: `docs` のパスだけを出典にせず、`source-migration-map.md` に各ソースの要求内容と移管先 ID を記録する。
- Decision: 新規要求で値が示されていないロード時間、履歴件数、viewer transport、AABB の正確な算出法を捏造しない。
- Decision: haptic feedback は利用者の表現に合わせて `SHOULD` とし、基礎受け入れの blocker にしない。
- Result: 要件移管は `READY`。実装開始は未解決事項と既存コードとの差分設計が必要なため `NOT-READY`。
