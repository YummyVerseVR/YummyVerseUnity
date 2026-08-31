# YummyService v2 API Contract Knowledge

## Authority Snapshot

今回の API 更新では、利用者が指定した `ru322/main` を規範 snapshot とする。

- Repository: `https://github.com/YummyVerseVR/YummyService`
- Branch: `ru322/main`
- Commit: `97c9ed75980ec398fe75159bd4e011b489112433`
- 確認日: 2026-08-30
- OpenAPI: `contracts/v2/openapi.yaml` (`openapi: 3.1.0`, `info.version: 2.0.0-draft`)
- OpenAPI SHA-256: `18462aa900a6b031438635fd46ddc997746c9782d6476247b1cb82c011409616`
- Workflow README SHA-256: `0a0ec28cae5a1607df8ff4b6dab379324d5d1b18255ed52b6feb3c79c94f199d`

この snapshot の OpenAPI は 104 paths、124 schemas を持ち、`YummyApiMock` と `YummyOrderServer` に v2 実装がある。従来確認していた `main@546b455...` の `paths: {}` snapshot は superseded された履歴であり、現行 contract の根拠にしない。

ただし `servers` は `https://vps.example.invalid/v2` の placeholder のままで、実運用の deployment host ではない。実運用の TLS、credentials、storage、GPU pipeline、受入環境は別途 qualification が必要である。Unity が実装する endpoint と schema の詳細は [`yummy-service-v2-unity-api.md`](./yummy-service-v2-unity-api.md) に集約する。

## Confirmed v2 Domain Contract

### Workflow DAG

```text
INPUT_MODERATION
|-- EXAMPLE_RETRIEVAL -- FOOD_ANALYSIS -- AUDIO_GENERATION
`-- IMAGE_TO_3D
```

| Stage | Direct dependencies |
|---|---|
| `INPUT_MODERATION` | none |
| `EXAMPLE_RETRIEVAL` | `INPUT_MODERATION` |
| `FOOD_ANALYSIS` | `EXAMPLE_RETRIEVAL` |
| `IMAGE_TO_3D` | `INPUT_MODERATION` |
| `AUDIO_GENERATION` | `FOOD_ANALYSIS` と、特定 Food Analysis revision の Admin `APPROVED` |

- Moderation が `PASS` または Admin override で承認されるまで downstream は開始しない。
- Moderation の `REVIEW` と `BLOCK` は別状態である。
- `EXAMPLE_RETRIEVAL` と `IMAGE_TO_3D` は moderation 後の独立 sibling である。
- Retrieval exhaustion は許可された Zero Shot の `COMPLETED_WITH_WARNING` として analysis を継続できる。一般的な failure を warning に変換する規則ではない。
- Food Analysis review は analysis/audio branch を保留し、独立した `IMAGE_TO_3D` は進行し得る。

### States and completion

- Stage: `PENDING`, `QUEUED`, `PROCESSING`, `COMPLETED`, `COMPLETED_WITH_WARNING`, `FAILED`, `CANCELED`。
- Order: `DRAFT`, `QUEUED`, `PROCESSING`, `AWAITING_ADMIN_REVIEW`, `COMPLETED`, `REJECTED`, `FAILED`, `CANCELED`。
- Moderation: `PASS`, `REVIEW`, `BLOCK`。
- Food Analysis status: `VALID`, `REVIEW_REQUIRED`。Admin decision は `APPROVED`, `REVIEW`。

`COMPLETED` には、承認済み moderation、retrieval の `COMPLETED` または許可された warning、承認済み Food Analysis、verified immutable `FOOD_ANALYSIS_JSON`、verified selected `GLB`、verified selected `WAV` が必要である。GLB が先に生成されても、Device の customer-safe projection では order 全体が `COMPLETED` になる前に downloadable として扱わない。

### Artifact and analysis schemas

- Artifact types: `SOURCE_IMAGE_ORIGINAL`, `SOURCE_IMAGE_NORMALIZED`, `FOOD_ANALYSIS_JSON`, `GLB`, `WAV`。
- `ArtifactRevision` の必須 field は `artifact_id`、`artifact_type`、`revision`、`sha256`、`verified`。`sha256` は64桁 hexadecimal string。
- Artifact revision は immutable。latest/current/selected は別 pointer であり、new revision の存在だけで selected とみなさない。
- `OrderInput.image` は `image/png`、`image/jpeg`、`image/webp`（animated WebP 不可）、raw 最大20 MiB。normalized image は最大5 MiB、最大1,500,000 pixels、aspect ratio 1:4〜4:1、拡大なしである。
- Food Analysis は food characteristics だけを持ち、texture 6軸（chewiness、firmness、elasticity、adhesiveness、brittleness、moisture、各1〜10）と構造化 `attributes` を使う。hardware/electrical control value は含めない。
- `confidence` は情報表示用であり、gameplay、haptic、電気制御の threshold に使わない。

### ProblemDetails

`application/problem+json` の `ProblemDetails` は `type`、`title`、`status` を必須とし、`detail`、`instance` は任意である。`status` は400〜599、unknown extension field は許容される。Unity は未知 extension を理由に response 全体を拒否せず、未知 enum・必須 field 欠落・wrong type・未検証 artifact は fail closed にする。

## Published HTTP Contract

現行 v2 では、次の route 群が OpenAPI と server/mock に公開されている。

- `admin`: food profile、hardware safety、order review、device token、menu、運用 kill switch 等。
- `customer`: order intake、status、artifact、image、submit/cancel 等。
- `worker`: job claim、input/artifact、complete/fail 等。
- `device`: Unity/Hardware の all-order status、artifact、payload、ACK。
- `public`: published sample menu と GLB/WAV sample。

認証 scheme は opaque customer/admin bearer、session cookie、device bearer に分離される。Unity Device の route は `deviceBearerAuth` を要求し、`device_type=UNITY` の provisioned token を使う。Admin token や Mock の static token を Unity build に埋め込まない。

Unity の正式な entry point は `GET /v2/devices/unity/orders` である。status、selected artifact download、Hardware Payload、Payload ACK の path・query・header・status・schema は専用文書に記録した。`/v2/menu` は認証不要の published sample read model であり、生成 order の履歴・認可・artifact revision を代替しない。

## Consumer Surface Status

| Capability | 現行 v2 の根拠 | Unity から見た状態 |
|---|---|---|
| Generated history | `GET /devices/unity/orders`、`DeviceOrderListResponse`、cursor/state/search/filter | route/schema は定義済み。deployment と product cache policy は未確定 |
| Order status | `GET /devices/unity/orders/{order_id}`、`CustomerOrderStatus` | route は定義済み。ただし analysis/GLB/WAV の sanitized projection のみで、全5 stage/review detail は返さない |
| Selected GLB/WAV | `CustomerOutputStatus.artifact_id` と device artifact download | completed + selected + verified の server gate はある。Unity response に checksum/revision metadata はない |
| Preview image | `SOURCE_IMAGE_NORMALIZED` domain artifact | Unity Device の preview operation/URL は未定義。public sample GLB/WAV を preview の代用にしない |
| Hardware Payload | `GET .../payload`、`POST .../payload/ack` | route/schema、ETag、Retry-After、ACK idempotency が定義済み。実運用の hardware policy は別 qualification |
| Auth | `deviceBearerAuth` と Admin device lifecycle | Unity の Device token contract は定義済み。Viewer/customer auth と deployed secret delivery は別課題 |
| Change detection | history cursor、payload `ETag`/`If-None-Match` | payload は conditional GET 可。order status の ETag/event/rate policy は未定義 |
| Integrity | `ArtifactRevision.sha256`、`HardwarePayload.payload_sha256` | domain metadata は存在するが、Unity artifact response に GLB/WAV checksum が露出しない |

従って、v2 は「HTTP path が無い」状態から「Unity Device の route/schema が公開された」状態へ進んだが、Unity の本番統合が全要件を満たしたことを意味しない。特に preview、全 stage の customer projection、artifact checksum、deployment host、実機認証は残課題である。

## V1 Retirement Policy

- **YummyVerseUnity から利用する v1 API は廃止済みであり、今後一切使用しない。**
- Production、development、test、demo、障害時 fallback、migration compatibility、Standalone の代替を含む全 runtime から `/v1/...` への outbound request を送らない。
- v1 client、DTO、endpoint configuration、server mock を runtime dependency として追加・維持しない。例外は v1 URL/response を拒否する local negative fixture だけである。
- Standalone は API request を行わない独立した local source であり、v1 fallback ではない。

## Current Unity Client Gap

現行 Unity の Network adapter は正式な Device API ではなく、開発用 Admin menu/sample API を参照している。後続実装では次を移行対象とする。

- `NetworkFoodCatalogSource` と `NetworkConnectionTester` は `/v2/admin/menu` と固定 `admin-demo-token` を使う。production は `/v2/devices/unity/orders` と `UNITY` Device token を使う。
- 現行 `MenuResponseDto` の `thumbnail_url`、`sample_audio_url`、`audio_url` は v2 `PublicMenuItem` の規範 field ではない。public sample の音声 field は `sample_wav_url` で、generated order status に preview URL はない。
- `NetworkFoodLoader` は menu URL を直接取得し、order/artifact identity、artifact revision、server checksum を保持しない。selected `artifact_id` を使う device download へ置き換える。
- `YummyServiceV2Contract` の local commit/hash constant は今回の snapshot より古い。contract snapshot 更新と client migration を同じ変更境界で扱う。

Standalone local catalog は Network API unavailable/auth failure/contract gap でも独立して継続し、API failure を理由に local item を消したり v1 route へ fallback したりしない。

詳細 schema、Unity の request/response 例、ACK/payload の fail-closed 規則、実装順序、外部 contract test は [`yummy-service-v2-unity-api.md`](./yummy-service-v2-unity-api.md) を参照する。
