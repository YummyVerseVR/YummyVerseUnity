# Contract Design Memory

- 2026-08-24: 利用者は YummyService repository と v2 使用を明示した。
- `main@546b455fedd205fb686ca7b93d6af596bced7879` の `contracts/v2/openapi.yaml` と `contracts/v2/README.md` を normative source として確認した。
- v2 OpenAPI は domain schema skeleton で、`paths` が空、server URL が `.invalid`、authentication/artifact lookup が deferred である。
- `feature/v2-analysis-review-timing-persistence` は domain/persistence code に差分があるが、normative `contracts/v2/` に差分はない。
- Current Unity client は QR GUID から旧 `/{guid}/model` を GET するため、v2 order/artifact contract へは endpoint replacement ではなく identity/state/cache boundary の再設計が必要である。
- Preview は `SOURCE_IMAGE_NORMALIZED` の利用を候補としたが、customer-visible selection/download response は未定義なので確定 endpoint/field としては扱わない。
- v2 completion には verified GLB だけでなく approved Food Analysis、retrieval、verified WAV/JSON が必要である。Unity は GLB が先に生成されても order `COMPLETED` 前に menu-ready としない方針を baseline にした。
- Food Analysis confidence は制御 threshold に使えないため、将来の食感/haptic parameter 自動化へ流用しない。
