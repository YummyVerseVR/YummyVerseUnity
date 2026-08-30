# Requirements Analysis Memory

- 旧チュートリアル仕様の QR event は `QrPlateDetected` だったが、新要求では食品 GUID 選択ではなく anchor designation の成立イベントとして扱う必要がある。
- S15〜S19 は旧仕様どおり TutorialSequence の外に置く一方、要件上は一つの来場者 journey として追跡した。
- 利用ガイドにだけ記載されていた `IGameCommandBus`、reset 対象、debug HUD、dummy event の撤去条件も、実装の偶然ではなく要件を守る境界として採用した。
- 画像先行読込と「即座に呼び出す」は両立させる必要がある。一覧表示は image/metadata のみ、3D は選択 item の cache 再利用または on-demand load としたが、数値 SLA は未定である。
- 外部 viewer が preview だけか 3D を含むかは決めず、共通 item identity と閲覧可能性だけを baseline にした。
- AABB の技術用語と「最遠2点」の組合せには解釈余地があるため、要求文を保持しつつ algorithm を open question にした。
- 食べカス、縮小、消滅は簡易表現であり、断面生成は明示的に除外した。
- 2026-08-30 refresh: YummyService `ru322/main@97c9ed7...` では Unity Device の HTTP operation が公開された。history/status/artifact/payload/ACK の正式 path と schema を使い、未公開の preview/full-stage/detail は推測しない。
- Current Unity integration はまだ `/v2/admin/menu` と固定 `admin-demo-token` の menu adapter であり、`Guid`/menu item URL は v2 Device の order ID/artifact ID/revision を表現できない。新 client boundary は opaque order identity と immutable ArtifactRef を必要とする。
- v2 order `COMPLETED` は verified GLB だけでなく、approved Food Analysis、retrieval、WAV、JSON を要求する。Device の `downloadable=true` も completed order の selected verified output に限定されるため、GLB の早期 customer visibility は許可しない。
- Unity Device status は `analysis`、GLB、WAV の sanitized projection だけで、全5 stage/review/failure detail を返さない。Unity artifact response に GLB/WAV の SHA-256/revision metadata がないため、client-side integrity gate は contract gap として保持する。
- Source image schema は定義されているが Unity app から order intake/upload/submit する要求はないため、現 scope の API operation へ追加しなかった。
- `deviceBearerAuth` は Unity Device API の認証 scheme である。`device_type=UNITY` の token lifecycle は公開されたが、deployment host/TLS/secret delivery と Physical Viewer/customer scope は未確定である。
- Canonical Experience Flow は既に S15〜S17 で Tutorial 後の食品選択 UI を持っていたが、Network/Standalone を一つの UI に同時表示することは独立した acceptance として弱かったため `FR34`/`FR35` を追加した。
- Standalone Mode は offline/local source として恒久維持し、Network availability や v2 contract readiness から独立させた。
- 2026-08-24 の利用者指示により、S3 の「目の前の紙皿を見つめてください」という案内と QR 検出待ちは TutorialSequence から削除する。Anchor designation の製品責務は残るが、guided tutorial の step/completion condition にはしない。
