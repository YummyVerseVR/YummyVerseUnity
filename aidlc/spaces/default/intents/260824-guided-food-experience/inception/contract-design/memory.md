# Contract Design Memory

- 2026-08-24: 利用者は YummyService repository と v2 使用を明示した。
- 2026-08-30 refresh: `ru322/main@97c9ed75980ec398fe75159bd4e011b489112433` の `contracts/v2/openapi.yaml` と `contracts/v2/README.md` を normative source として確認した。OpenAPI は104 paths/124 schemasである。
- Unity Device の history/status/artifact/payload/ACK path、`deviceBearerAuth`、主要 request/response/status は公開済み。ただし `servers.url` は `.invalid` placeholder のままである。
- Current Unity client はまだ `/v2/admin/menu` と固定 `admin-demo-token` の menu adapter を使うため、formal v2 Device API へは endpoint replacement だけでなく identity/state/cache boundary の再設計が必要である。
- Preview は `SOURCE_IMAGE_NORMALIZED` の domain type まで定義されているが、Unity Device の customer-visible selection/download operation は未定義。public sample menu を preview/order artifact の代用にしない。
- Unity Device status は全5 stage/review/failure detail と selected revision/sha256 を返さない。GLB/WAV download にも checksum metadata/header がないため、full consumer contract と client-side integrity gate は未達である。
- v2 completion には verified GLB だけでなく approved Food Analysis、retrieval、verified WAV/JSON が必要である。Unity は GLB が先に生成されても order `COMPLETED` 前に menu-ready としない方針を baseline にした。
- Food Analysis confidence は制御 threshold に使えないため、将来の食感/haptic parameter 自動化へ流用しない。
