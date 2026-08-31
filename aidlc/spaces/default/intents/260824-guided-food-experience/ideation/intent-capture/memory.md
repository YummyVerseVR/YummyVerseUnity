# Intent Capture Memory

- 2026-08-24: 利用者はドキュメントを `aidlc` に一本化し、`docs` 内 Markdown の未反映要件を移管するよう要求した。
- 2026-08-24: 利用者は、将来 `docs` が削除されても要件が理解できる自己完結性を明示的な完了条件に追加した。
- `docs/tutorial-requirement.md` は同じ仕様が重複して連結された状態だが、重複を要件の追加とは扱わず、一つの仕様として正規化した。
- `docs/tutorial-usage.md` は操作説明を中心とするが、イベント/コマンド境界、リセット責務、データ編集性、デバッグ、禁止事項などの規範的内容を要件へ取り込んだ。
- 2026-08-24 の追加要求は、旧 intent の「QR GUID による食品選択を維持する」判断と競合する。履歴は残し、新 intent を現行要件として supersede させる。
- 画像プレビューの先行読込は、メニュー表示のために 3D モデルを先行ロードしない、という意味で記録した。選択後のモデル取得・展開方法と数値性能目標は未指定である。
- 「コントローラーが震えてくれると嬉しいかも」は必須受け入れ条件ではなく `SHOULD` 要件として記録した。
- 2026-08-24: 利用者はアプリが呼ぶ API repository として YummyService を指定し、利用 version を v2 と明示した。
- 2026-08-24 時点では v2 normative OpenAPI に path がなかったため、API repository の v1 route や Unity の旧 GUID route を流用せず、必要 capability と contract publication gate を追加した。
- 2026-08-30 refresh: `ru322/main` で Unity Device の history/status/artifact/payload/ACK path が公開された。現在はその schema と残る preview/full-stage/checksum/deployment gap を後続実装の根拠とする。
- 2026-08-24: 利用者は Standalone Mode を今後も使用し、Tutorial 完了後の食品一覧 UI に v2 API 食品と端末保存済み食品の両方を表示するよう明示した。
- Standalone は v1 fallback ではない。API request を行わない独立 source として v1 廃止方針と両立させる。
