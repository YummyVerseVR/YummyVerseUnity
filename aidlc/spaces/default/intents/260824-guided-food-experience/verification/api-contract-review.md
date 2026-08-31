# API Contract Review: YummyService v2

## Source

- Repository: `https://github.com/YummyVerseVR/YummyService`
- Branch/commit: `ru322/main@97c9ed75980ec398fe75159bd4e011b489112433`
- Review date: 2026-08-30
- Normative files: `contracts/v2/openapi.yaml`, `contracts/v2/README.md`
- OpenAPI: `3.1.0` / contract version `2.0.0-draft`
- Contract size: 104 paths、124 schemas

## Source Integrity

| File | SHA-256 |
|---|---|
| `contracts/v2/openapi.yaml` | `18462aa900a6b031438635fd46ddc997746c9782d6476247b1cb82c011409616` |
| `contracts/v2/README.md` | `0a0ec28cae5a1607df8ff4b6dab379324d5d1b18255ed52b6feb3c79c94f199d` |

## Checks Performed

- [x] OpenAPI version is `3.1.0`; contract version is `2.0.0-draft`.
- [x] `servers.url` remains placeholder `https://vps.example.invalid/v2`; it is not a production deployment URL.
- [x] v2 `paths` is no longer empty. The snapshot defines 104 paths and 124 schemas.
- [x] Unity Device routes exist: history, order status, selected GLB/WAV artifact download, Hardware Payload, and Payload ACK.
- [x] Unity Device routes require `deviceBearerAuth`; Admin device lifecycle defines `UNITY` token issue/rotate/revoke. Mock static tokens remain development-only.
- [x] `DeviceOrderListResponse`, `CustomerOrderStatus`, `CustomerOutputStatus`, `HardwarePayload`, `DevicePayloadNotReady`, `HardwarePayloadAckRequest`, `HardwarePayloadAck`, `PublicMenuItem`, and `ProblemDetails` were reviewed.
- [x] History query semantics were reviewed: state/search/food-name filters, limit 1〜100, opaque signed cursor, stable `created_at DESC`/`order_id DESC` ordering.
- [x] Payload semantics were reviewed: `202` + `Retry-After`, `200` + `ETag`, `304` + `If-None-Match`, fail-closed non-READY statuses, and `503` kill switch.
- [x] ACK semantics were reviewed: required `Idempotency-Key`, `201` new, `200` replay, and `409` conflicting reuse.
- [x] Artifact download semantics were reviewed: only selected verified GLB/WAV from a `COMPLETED` order, with `model/gltf-binary` or `audio/wav`.
- [x] Public `/menu` was distinguished from Unity Device generated-order API. It exposes only `PublicMenuItem` sample GLB/WAV URLs; it has no generated-order history or preview image.
- [x] Domain workflow DAG, all OrderState/StageState values, completion gate, source image, Food Analysis, ArtifactRevision, and ProblemDetails were retained from the prior review and rechecked against the new snapshot.
- [x] `YummyApiMock/tests/test_v2_unity.py` and `YummyOrderServer/tests/test_v2_device_api.py` were identified as Unity/device contract evidence.
- [x] Current Unity API boundary was reviewed: `NetworkFoodCatalogSource`, `NetworkFoodLoader`, `NetworkConnectionTester`, `MenuResponseDto`, and `YummyServiceV2Contract`.
- [x] Current Unity code still uses `/v2/admin/menu` with fixed `admin-demo-token`; it has not migrated to the formal Unity Device API.
- [x] Current Unity `MenuResponseDto` fields `thumbnail_url`/`sample_audio_url`/`audio_url` are not normative `PublicMenuItem` fields; `sample_wav_url` is the current public sample audio field.
- [x] v1 API and legacy `/{guid}/model` remain prohibited; no fallback was inferred from the new routes.

## Unity-Relevant Findings

| Area | Current contract | Assessment |
|---|---|---|
| History | `GET /v2/devices/unity/orders` → `DeviceOrderListResponse` | Route/schema ready; no preview URL and deployment/cache policy remain |
| Status | `GET /v2/devices/unity/orders/{order_id}` → sanitized `CustomerOrderStatus` | Route ready; full five-stage/review/failure projection is not exposed |
| GLB/WAV | `GET /v2/devices/unity/orders/{order_id}/artifacts/{artifact_id}/download` | Media type and selected/verified/completed gate ready; checksum/revision are not returned to Unity |
| Payload | `GET .../payload`, `POST .../payload/ack` | Schema, conditional request, retry, and idempotency behavior ready; runtime deployment pending |
| Preview | Normalized image is a domain artifact | No Unity Device preview operation; current requirement cannot be fulfilled by `/menu` sample routes |
| Integrity | `ArtifactRevision.sha256` exists; `HardwarePayload.payload_sha256` is payload-only | GLB/WAV bytes cannot currently be checked against a server-provided digest in the Unity response |
| Compatibility | Snapshot commit/hash is known | No deployed runtime negotiation endpoint/field is defined |

## Result

- v2 domain vocabulary/schema mapping: `READY`
- v2 Unity Device route/schema: `READY`
- v2 Unity full consumer contract: `PARTIAL`
- v2 production HTTP integration: `NOT-READY`
- v1 usage: `FORBIDDEN`

Production integration remains blocked by the placeholder deployment URL, real token/TLS/device qualification, missing Unity preview operation, limited customer status projection, missing GLB/WAV checksum exposure, and incomplete change-detection/retry/compatibility policy. The new route definitions are sufficient to begin a dedicated adapter design, but do not authorize replacing the current code with guessed public-menu or legacy routes.

## Follow-up Gate

Before accepting Unity production integration:

1. Bind the adapter to a real deployment host and `UNITY` Device token delivery mechanism without embedding secrets.
2. Add contract fixtures for `DeviceOrderListResponse`, readiness filtering, cursor handling, ProblemDetails, Payload `ETag`/`Retry-After`, and ACK idempotency.
3. Resolve preview and GLB/WAV integrity metadata in the YummyService contract, or explicitly revise FR30/FR31.
4. Run Mock/server contract tests plus Unity EditMode/PlayMode and Quest/PCVR network tests.
