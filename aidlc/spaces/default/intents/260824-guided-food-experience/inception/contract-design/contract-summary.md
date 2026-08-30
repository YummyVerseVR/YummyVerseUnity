# Contract Summary: YummyService v2

## Scope

YummyVerseUnity が YummyService v2 の生成 order を履歴から選び、selected GLB/WAV を取得し、必要に応じて Unity Device 用 Hardware Payload を扱うための consumer contract を記録する。iPad 等の Physical Menu Viewer は同じ identity を共有するが、viewer 固有の認証・表示範囲は別途確定する。

## Authority

- Source repository: `https://github.com/YummyVerseVR/YummyService`
- Branch: `ru322/main`
- Reviewed commit: `97c9ed75980ec398fe75159bd4e011b489112433`
- Reviewed: 2026-08-30
- OpenAPI: `contracts/v2/openapi.yaml` (`openapi: 3.1.0`, `info.version: 2.0.0-draft`)
- OpenAPI SHA-256: `18462aa900a6b031438635fd46ddc997746c9782d6476247b1cb82c011409616`
- Workflow README SHA-256: `0a0ec28cae5a1607df8ff4b6dab379324d5d1b18255ed52b6feb3c79c94f199d`
- OpenAPI は 104 paths、124 schemas。従来の `main@546b455...` / `paths: {}` snapshot は superseded。
- 詳細な Unity request/response schema は `knowledge/aidlc-shared/yummy-service-v2-unity-api.md`、全体 domain contract は `knowledge/aidlc-shared/yummy-service-v2-api.md` に記録する。

`servers.url` は `https://vps.example.invalid/v2` の placeholder で、実運用 host ではない。したがって route/schema は実装可能になったが、production deployment、TLS、secret delivery、実機 acceptance は未完了である。

## Unity Device API

| 用途 | Path | Response / status | Unity rule |
|---|---|---|---|
| Generated history | `GET /v2/devices/unity/orders` | `200 DeviceOrderListResponse`; `400/401/403` | `state`, `q`, `food_name`, `limit`、opaque `cursor` を使う。sort は `created_at DESC`, `order_id DESC` |
| Order status | `GET /v2/devices/unity/orders/{order_id}` | `200 CustomerOrderStatus`; `401/403/404` | `state` と `analysis/glb/wav` projection を読む。未定義の stage/detail を推測しない |
| Selected artifact | `GET /v2/devices/unity/orders/{order_id}/artifacts/{artifact_id}/download` | `200 model/gltf-binary` または `audio/wav`; `401/403/404` | status の selected `artifact_id` だけを取得する。completed・verified・selected gate は server 側で適用 |
| Hardware Payload | `GET /v2/devices/unity/orders/{order_id}/payload` | `200`, `202`, `304`, `401/403/404`, kill switch `503` | `READY` 以外を制御値として使わない。`ETag`/`If-None-Match`/`Retry-After` を扱う |
| Payload ACK | `POST /v2/devices/unity/orders/{order_id}/payload/ack` | `201` new、`200` idempotent replay、`400/401/403/404/409` | 同じ Device・key・body で timeout 後に再送する |

全 Unity Device route は `deviceBearerAuth`、すなわち `Authorization: Bearer <opaque-unity-device-token>` を要求する。token は Admin の device lifecycle で `device_type=UNITY` として発行し、Admin/worker/Mock static token を build に埋め込まない。

## Consumer-to-Domain Mapping

| Product concept | v2 contract | Consumer rule / current gap |
|---|---|---|
| Generated food/history | `DeviceOrderListResponse.items[]` の `CustomerOrderStatus` | `order_id` を opaque identity として保持する。QR GUID や menu item ID を order identity にしない |
| Order readiness | `OrderState` と `CustomerOutputStatus` | `COMPLETED`、`glb.downloadable=true`、`glb.artifact_id` が揃った item だけを GLB candidate にする |
| Stage progress | domain の `Stage`/`StageState` | Device status は `analysis`、GLB、WAV の sanitized projection のみ。全5 stage、moderation/retrieval/I23D、review/failure detail は未公開 |
| Preview image | domain `SOURCE_IMAGE_NORMALIZED` | Unity Device に preview operation/URL がない。public sample GLB/WAV を preview の代用にしない |
| 3D model | selected verified `GLB` download route | route はあるが response に `sha256`/revision/checksum header がないため、FR31 の byte verification は未達 |
| Eating sound | selected verified `WAV` download route | status の `wav.artifact_id` を使う。public sample WAV は generated order artifact と別物 |
| Hardware control | `HardwarePayload` | `READY` かつ値が明示された場合だけ適用。`UNSUPPORTED`/`AMBIGUOUS`/`UNSAFE`/`INVALIDATED` は fail closed |
| Failure/retry | `ProblemDetails`、operation-specific status | `401/403/404/409/429/5xx`、timeout/cancel を分類し、未知 extension は許容する |

## Confirmed Invariants

- YummyVerseUnity では v1 API と旧 `/{guid}/model` を production、development、test、demo、fallback、migration compatibility を問わず使用しない。v1 rejection 用 local negative fixture だけを許容する。
- OrderState は `DRAFT`, `QUEUED`, `PROCESSING`, `AWAITING_ADMIN_REVIEW`, `COMPLETED`, `REJECTED`, `FAILED`, `CANCELED` を区別する。StageState は `PENDING`, `QUEUED`, `PROCESSING`, `COMPLETED`, `COMPLETED_WITH_WARNING`, `FAILED`, `CANCELED` を区別する。
- Workflow は `INPUT_MODERATION` から retrieval→analysis→audio と I23D に分岐する。`AWAITING_ADMIN_REVIEW` は全 branch 停止を意味しない。
- Order `COMPLETED` は approved moderation/analysis、許可された retrieval outcome、verified immutable JSON/GLB/WAV を要求する。早期 GLB を customer-ready としない。
- `ArtifactRevision` は `artifact_id`、`artifact_type`、`revision`、`sha256`、`verified` を持つ immutable identity である。selected/current/latest pointer は別に扱う。
- Food Analysis の `confidence` から gameplay、haptic、電気刺激の制御値を自動導出しない。
- Standalone は API request を行わない第一級 local source であり、Network failure 時も利用可能な item を維持する。

## Required Consumer Capabilities and Current Status

| ID | Capability | 現行 v2 の状態 |
|---|---|---|
| `API-CAP-01` | Generated history query | `GET /v2/devices/unity/orders` と cursor/state/search/filter は定義済み。preview metadata は未提供 |
| `API-CAP-02` | One order/status | `CustomerOrderStatus` は定義済み。ただし全5 stage、review/failure detail、selected revision は不足 |
| `API-CAP-03` | Selected artifact metadata | Admin/domain schema には存在するが Unity Device projection に `revision`/`sha256` 等がなく部分達成 |
| `API-CAP-04` | Immutable preview download | normalized source image の Unity operation が未定義 |
| `API-CAP-05` | Immutable GLB/WAV download | Device artifact route と media type は定義済み。checksum/size/range/retry の client contract は不足 |
| `API-CAP-06` | Unity/device authentication | `deviceBearerAuth`、`UNITY` device token 発行/rotate/revoke が定義済み。deployed secret delivery は未達 |
| `API-CAP-07` | Change detection | history cursor と payload ETag はある。order status の ETag/event、rate limit/cache policy は未定義 |
| `API-CAP-08` | Operation problems | `ProblemDetails` と route ごとの主要 status は定義済み。retryability の全運用方針は未確定 |
| `API-CAP-09` | Compatibility detection | commit/schema は snapshot で固定可能だが、deployed server の runtime negotiation は未定義 |

## Endpoint Readiness Matrix

| Capability | Schema | HTTP path/method | Auth | Mock/server evidence | Status |
|---|---:|---:|---:|---:|---|
| Order/stage vocabulary | Yes | Yes/domain | `deviceBearerAuth` | v2 tests | `READY` |
| History query | Yes | Yes | Unity Device | v2 tests | `PARTIAL` |
| Order status | Yes | Yes | Unity Device | v2 tests | `PARTIAL` |
| Selected artifact metadata | Yes/domain | Download path only | Unity Device | selected-artifact tests | `PARTIAL` |
| Preview download | Domain type only | No Unity path | N/A | No | `NOT-READY` |
| GLB/WAV download | Yes/media type | Yes | Unity Device | selected verified download tests | `PARTIAL` |
| Hardware Payload/ACK | Yes | Yes | Unity Device | payload/ACK tests | `READY` schema / deployment pending |
| Viewer/customer auth | Partial | Customer paths exist | bearer/cookie | not Unity viewer acceptance | `PARTIAL` |
| Problem payload | Yes | Operation mappings exist | per route | route tests | `PARTIAL` |

## Client Adapter Contract

- `IYummyServiceV2Client` 等の専用境界で transport DTO と application domain を隔離する。
- `order_id`、`artifact_id`、payload revision は opaque string として扱い、`Guid` へ変換しない。Artifact cache identity は metadata が取得できる場合 `artifact_id + revision + sha256` とする。
- `CustomerOutputStatus.artifact_id` が無い item を guessed URL や public menu URL で補完しない。
- 未知 enum、必須 field 欠落、wrong media type、`verified=false`、checksum 不一致を success/load へ変換しない。
- Cursor は opaque のまま保存・再送し、filter を変えた cursor を再利用しない。
- Payload は `READY` の明示値だけを適用し、ACK は `Idempotency-Key` と同じ body で再送する。
- ProblemDetails の未知 extension field は無視できるが、status と operation を UI/log/retry policy に反映する。
- Network と Standalone は別 adapter、identity namespace、failure policy を持つ。一方の失敗で他方を停止しない。

## Blocking Contract Gaps

1. Unity Device status は全 stage/review/failure detail と selected artifact metadata を返さない。
2. Unity Device に normalized preview image の operation がない。
3. GLB/WAV download に client が照合できる `sha256`/revision metadata または checksum header がない。`payload_sha256` は GLB/WAV digest ではない。
4. `servers` が placeholder で、production host、TLS、token 配布、実機/device acceptance が未確定である。
5. Order status の change detection、rate limit、cache/retry policy と deployed compatibility negotiation が未定義である。

以上が解消されるまで、Unity の本番 HTTP integration と FR31/preview/full-stage の合格判定は保留する。v1 endpoint や `/v2/menu` public sample を代替実装として採用しない。

## Verification Contract

- `contracts/v2/openapi.yaml` の OpenAPI 3.1 validation と snapshot hash を確認する。
- YummyApiMock の `YummyApiMock/tests/test_v2_unity.py` と YummyOrderServer の `YummyOrderServer/tests/test_v2_device_api.py` を、list filter/cursor、role boundary、payload 202/200/304、fail-closed state、ACK replay/conflict、selected verified download の根拠とする。
- Unity 側では `CustomerOrderStatus` の readiness、unknown enum、missing artifact ID、wrong type、public sample 混入、ProblemDetails、cursor、Payload ETag/ACK idempotency の fixture を追加する。
- production acceptance には deployment host/TLS、実 token lifecycle、GLB/WAV byte integrity、preview、Quest/PCVR の実機確認を追加する。

## Review

- Domain contract mapping: `READY`
- Unity Device route/schema: `READY`（production deployment は未 qualification）
- Unity full consumer contract: `PARTIAL`
- Production HTTP integration: `NOT-READY`
- Basis: `ru322/main@97c9ed7...` で route/schema/Mock/server は追加されたが、preview、全 stage projection、artifact checksum、deployment/runtime negotiation が未解決である。
