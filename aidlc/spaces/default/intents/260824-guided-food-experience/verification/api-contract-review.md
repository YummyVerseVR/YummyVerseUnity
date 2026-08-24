# API Contract Review: YummyService v2

## Source

- Repository: `https://github.com/YummyVerseVR/YummyService`
- Branch/commit: `main@546b455fedd205fb686ca7b93d6af596bced7879`
- Review date: 2026-08-24
- Normative files: `contracts/v2/openapi.yaml`, `contracts/v2/README.md`

## Source Integrity

| File | SHA-256 |
|---|---|
| `contracts/v2/openapi.yaml` | `73a21a380d23a136f92ddea7bc45cfcc9556aac040f5aa9a9e1c58d34ac0f5f0` |
| `contracts/v2/README.md` | `e3f6635bf215b2e96b6005d8946fe5c6b0549f8db995efa11b2ac9139d91e46a` |

## Checks Performed

- [x] OpenAPI version is `3.1.0`; contract version is `2.0.0-draft`.
- [x] Server URL is placeholder `https://vps.example.invalid/v2`.
- [x] Top-level `security` is empty, while README explicitly defers authentication; anonymous production access was not inferred.
- [x] `paths` is an empty object; callable v2 HTTP operations are not yet defined.
- [x] Workflow DAG, order/stage/moderation/analysis states, completion gate, source image, Food Analysis, ArtifactRevision, ProblemDetails were reviewed.
- [x] YummyOrderServer/YummyApiMock route scan found `/v1/...` routes and no `/v2/...` HTTP route.
- [x] `feature/v2-analysis-review-timing-persistence` was compared with reviewed `main`; normative `contracts/v2/` had no difference.
- [x] Current Unity API boundary was reviewed: `FoodDownloader.cs`, `FoodContext.cs`, `IFoodFetchable.cs`, `FoodDownloadResult.cs`, `EndPointManager.cs`.
- [x] Current Unity client uses legacy GUID-triggered `/{guid}/model`, not v2 order/artifact contract.
- [x] User clarified that v1 is retired and must never be used; requirements prohibit every runtime outbound v1 request and allow only local negative rejection fixtures.

## Result

- v2 domain vocabulary/schema mapping: `READY`
- v2 HTTP endpoint/auth/artifact integration: `NOT-READY`
- v1 usage: `FORBIDDEN`
- Blocking evidence: empty v2 `paths`, placeholder deployment URL, deferred authentication/artifact lookup/download/customer/device token.
- Next contract gate: YummyService normative v2 OpenAPI must define `API-CAP-01`〜`API-CAP-09` and corresponding mock/server contract tests before YummyVerseUnity production integration can be accepted.
