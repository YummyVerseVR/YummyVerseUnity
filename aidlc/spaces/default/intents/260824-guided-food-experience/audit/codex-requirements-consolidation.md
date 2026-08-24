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

## 2026-08-24 — YummyService v2 Contract Review

- Input: `https://github.com/YummyVerseVR/YummyService`、利用 API version は v2 という利用者指定。
- Evidence: `main@546b455fedd205fb686ca7b93d6af596bced7879` の `contracts/v2/openapi.yaml` と `contracts/v2/README.md`、現行 Unity `FoodDownloader`/`FoodContext`/`IFoodFetchable`/`EndPointManager`。
- Decision: v2 domain vocabulary、workflow、state、completion gate、immutable artifact/SHA-256、ProblemDetails を `FR25`〜`FR33` と contract design に反映する。
- Decision: v2 OpenAPI に path がなく、server URL/auth/artifact lookup/download が未確定なため、HTTP endpoint を推測しない。
- Decision: v1 routes と旧 `/{guid}/model` を v2 fallback として使用しない。
- Decision: Quest/Physical Viewer に必要な9つの API capability を path 非依存で定義し、normative v2 OpenAPI への追加を Construction gate とする。
- Decision: Contract source commit/version/checksum を shared knowledge に固定し、draft 更新は diff review 後に採用する。
- Result: v2 domain mapping は `READY`、production HTTP integration は `NOT-READY`。

## 2026-08-24 — V1 Retirement Clarification

- Input: 利用者は v1 API が廃止され、金輪際使われない旨を明記するよう要求した。
- Decision: YummyVerseUnity の全 runtime/environment/fallback から v1 API outbound request を恒久的に禁止する。
- Scope: Production、development、test、demo、failure fallback、migration compatibility、Standalone alternative を含む。
- Exception: v1 URL/response を拒否できることを検証する local negative fixture だけ。v1 server への接続は許可しない。
- Result: `FR25`、project memory、shared contract knowledge、contract summary を強化した。

## 2026-08-24 — Standalone and Unified Selection UI Clarification

- Input: 利用者は Standalone Mode を今後も使用し、Tutorial 完了後の食品一覧 UI に API 食品と local Standalone 食品の両方を表示するよう要求した。
- Finding: Game flow は `Canonical Experience Flow` の S1〜S19 として既に存在し、S15〜S17 に post-tutorial menu/selection/provision が記録されていた。
- Gap: `YummyService v2/Standalone catalog` という表記は存在したが、一つの UI への同時統合、source 識別、API failure 時の local 継続が独立した acceptance として不足していた。
- Decision: `FR34` に unified post-tutorial food selection UI、`FR35` に API 非依存 Standalone continuity を追加する。
- Decision: Standalone は v1 fallback ではなく、API request を行わない第一級 local source とする。
