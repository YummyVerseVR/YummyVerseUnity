# Contract Summary: YummyService v2

## Scope

YummyVerseUnity と iPad 等の Physical Menu Viewer が、YummyService v2 の生成 order と immutable artifact を利用して、履歴一覧、画像 preview、選択 GLB の表示を行うための consumer contract を定義する。

## Authority

- Source repository: `https://github.com/YummyVerseVR/YummyService`
- Version requested by product owner: `v2`
- Reviewed normative snapshot: `main@546b455fedd205fb686ca7b93d6af596bced7879`
- OpenAPI contract: `contracts/v2/openapi.yaml`, `2.0.0-draft`
- Workflow contract: `contracts/v2/README.md`
- Self-contained contract facts: `knowledge/aidlc-shared/yummy-service-v2-api.md`

## Consumer-to-Domain Mapping

| Product concept | v2 domain concept | Consumer rule |
|---|---|---|
| Generated food/history item | Order + customer-visible metadata | Stable item identity must map to order identity; do not use QR GUID |
| Generation state | OrderState + five StageState values | Show order and stage progress separately |
| Review pending | `AWAITING_ADMIN_REVIEW` plus review stage/decision | Not selectable as ready; explain pending review; independent I23D may still progress |
| Preview image | Selected/visible `SOURCE_IMAGE_NORMALIZED` revision | Preload image only; verify identity/integrity; visibility endpoint is not yet defined |
| 3D model | Selected verified `GLB` artifact revision | Load only after order `COMPLETED`; verify SHA-256 before glTF load |
| Eating sound | Selected verified `WAV` artifact revision | Contract requires WAV for order completion; playback requirement remains a separate product decision |
| Food display name/details | Customer-visible order input or approved Food Analysis projection | Exact response field is not exposed by a v2 endpoint yet |
| Failure | Order/Stage terminal state + ProblemDetails | Preserve distinction among rejected, failed, canceled, warning, review |

## Confirmed Invariants

- YummyVerseUnity では v1 API は廃止済みであり、今後一切使用しない。全 runtime/environment/fallback で outbound `/v1/...` request を禁止し、v1 rejection 用 negative fixture だけを許容する。
- Only v2 vocabulary is accepted for the integration; no silent or explicit v1 fallback.
- Menu-ready food requires `OrderState.COMPLETED` and a selected `ArtifactType.GLB` with `verified=true` and valid SHA-256.
- Artifact revisions are immutable. Cache identity includes `artifact_id`, `revision`, and `sha256`; latest/current is a separate pointer.
- `COMPLETED_WITH_WARNING` is a successful stage outcome only where the workflow permits it; it is not equivalent to generic success for every stage.
- `AWAITING_ADMIN_REVIEW` is not a claim that Image-to-3D has stopped.
- Food Analysis `confidence` must never automatically determine game, haptic, or electrical-control behavior.
- ProblemDetails permits extension properties and client parsing must be forward-compatible.

## Required API Operations

The following are required capabilities, not currently assigned paths:

| Capability ID | Operation needed by app/viewer | Minimum response contract |
|---|---|---|
| `API-CAP-01` | Query generated history | Stable order/item ID, display metadata, OrderState, preview readiness, cursor/page token |
| `API-CAP-02` | Get one order/status | OrderState, all StageState entries, warning/review/failure details, selected revisions |
| `API-CAP-03` | Get selected artifact metadata | Artifact ID/type/revision/SHA-256/verified/media type/size/download capability |
| `API-CAP-04` | Download immutable preview | Authorized binary response for normalized source image with media type/integrity/cache semantics |
| `API-CAP-05` | Download immutable GLB | Authorized binary response with integrity, redirect/range/timeout semantics |
| `API-CAP-06` | Authenticate Quest/viewer | Client-appropriate token scope/lifetime/refresh/revocation; no admin/worker secret |
| `API-CAP-07` | Detect history/order change | Poll/conditional GET/event contract plus rate/retry guidance |
| `API-CAP-08` | Report operation problem | `application/problem+json`, operation-specific statuses, retryability |
| `API-CAP-09` | Detect v2 compatibility | Deployed API/contract version or equivalent capability negotiation |

## Endpoint Readiness Matrix

| Capability | v2 domain schema | HTTP path/method | Auth | Runtime implementation | Status |
|---|---:|---:|---:|---:|---|
| Order/stage vocabulary | Yes | No | Deferred | Domain/persistence partial | `DOMAIN-READY` |
| Artifact metadata | Yes | No | Deferred | Persistence partial | `TRANSPORT-NOT-READY` |
| History query | No response schema | No | Deferred | No v2 route | `NOT-READY` |
| Preview download | Type/integrity only | No | Deferred | No v2 route | `NOT-READY` |
| GLB download | Type/integrity only | No | Deferred | No v2 route | `NOT-READY` |
| Quest/iPad auth | No | No | Deferred | No v2 route | `NOT-READY` |
| Problem payload | Generic schema | No operation mapping | Deferred | No v2 route | `PARTIAL` |

## Client Adapter Contract

- `IYummyServiceV2Client` など専用境界で transport DTO と application domain を隔離する。
- v1 client/DTO/configuration/mock を runtime dependency に含めず、v1 URL/response を受け取った場合は Contract Guard が拒否する。
- `GeneratedFoodItemId` は QR GUID/LocalFoods enum と分離し、server order identity を保持できる opaque value とする。
- `ArtifactRef` は artifact ID/type/revision/SHA-256/verified を一組で保持する。
- Preview Repository と Selected Model Loader は同じ client を使うが、preview/model の cache と download lifecycle を分ける。
- CancellationToken を全 request/download/verify/load に伝播する。
- API DTO の enum 値が未知の場合、`COMPLETED` や selectable item に推測変換せず compatibility error として fail closed する。
- ProblemDetails の未知 extension field は無視/保持でき、必須 field と status を使って UI/log/retry policy を決める。

## Blocking Contract Gaps

現時点の `paths: {}` と deferred authentication/artifact lookup のため、`API-CAP-01`〜`API-CAP-09` は URL/path/method/headers/body/response/status の implementable contract を持たない。Unity code へ実 URL をハードコードしたり、v1 endpoint shape を v2 として流用したりしてはならない。

Construction を開始するには、YummyService v2 の normative OpenAPI に必要 operation が追加され、少なくとも mock/server の contract test が通ることを phase gate とする。

## Verification Contract

- OpenAPI 3.1 validation と、reviewed commit/version の記録。
- Contract fixture による全 OrderState/StageState/ArtifactType/ProblemDetails mapping test。
- `COMPLETED` + verified selected GLB だけが selectable になる test。
- SHA-256 mismatch、unknown enum、missing selected GLB、review/failed/canceled order の fail-closed test。
- Preview 一覧で GLB download が発生しない integration test。
- Quest/iPad の auth scope と同じ catalog identity を確認する end-to-end test。

## Review

- Domain contract understanding: `READY`
- HTTP integration contract: `NOT-READY`
- Reason: v2 normative OpenAPI is `2.0.0-draft` with zero paths, invalid placeholder server URL, and deferred auth/artifact lookup.
