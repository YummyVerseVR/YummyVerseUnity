# YummyService v2 Unity API Schema

## この文書の位置づけ

YummyVerseUnity が後続実装で参照する YummyService v2 の Unity 向け抜粋である。規範ソースは次の snapshot とする。

- Repository: `https://github.com/YummyVerseVR/YummyService`
- Branch: `ru322/main`
- Commit: `97c9ed75980ec398fe75159bd4e011b489112433`
- 確認日: 2026-08-30
- OpenAPI: `contracts/v2/openapi.yaml` (`openapi: 3.1.0`, `info.version: 2.0.0-draft`)
- OpenAPI SHA-256: `18462aa900a6b031438635fd46ddc997746c9782d6476247b1cb82c011409616`
- Workflow README SHA-256: `0a0ec28cae5a1607df8ff4b6dab379324d5d1b18255ed52b6feb3c79c94f199d`

この commit では OpenAPI に 104 paths、124 schemas が定義され、`YummyApiMock` と `YummyOrderServer` に v2 の実装がある。ただし `servers` の URL は引き続き `https://vps.example.invalid/v2` という placeholder であり、実運用の接続先を示さない。Mock の token も開発専用である。

全体の workflow/API 方針は [`yummy-service-v2-api.md`](./yummy-service-v2-api.md)、この文書の元 contract は YummyService の [`contracts/v2/openapi.yaml`](https://github.com/YummyVerseVR/YummyService/blob/ru322/main/contracts/v2/openapi.yaml) と [`contracts/v2/README.md`](https://github.com/YummyVerseVR/YummyService/blob/ru322/main/contracts/v2/README.md) を参照する。

## Unity が使う API の全体像

```text
GET  /v2/devices/unity/orders
  └─ GET /v2/devices/unity/orders/{order_id}
       ├─ GET /v2/devices/unity/orders/{order_id}/artifacts/{artifact_id}/download
       └─ GET /v2/devices/unity/orders/{order_id}/payload
            └─ POST /v2/devices/unity/orders/{order_id}/payload/ack
```

`/devices/unity` は trusted Unity Device 用の all-order API である。`/menu` は公開された開発用サンプルメニューであり、生成 order の履歴・認可・artifact revision を代替しない。

| 用途 | v2 operation | Unity での扱い |
|---|---|---|
| 生成履歴 | `GET /v2/devices/unity/orders` | 一覧・検索・ページング。`state=COMPLETED` などで候補を絞る |
| order status | `GET /v2/devices/unity/orders/{order_id}` | 表示名、order state、analysis/GLB/WAV の状態を取得 |
| 完成 artifact | `GET /v2/devices/unity/orders/{order_id}/artifacts/{artifact_id}/download` | status が返した selected artifact の GLB/WAV だけを取得 |
| Hardware Payload | `GET /v2/devices/unity/orders/{order_id}/payload` | `READY` の control values を取得。未準備は polling |
| Payload ACK | `POST /v2/devices/unity/orders/{order_id}/payload/ack` | 適用した payload revision を idempotent に通知 |

## 認証と token

Unity API の security scheme は `deviceBearerAuth` である。

```http
Authorization: Bearer <opaque-unity-device-token>
```

- token は opaque な文字列で、Customer token、Admin token、Hardware Device token と共有しない。
- Device の `device_type` は `UNITY`。Hardware token を Unity endpoint に送ると `403`、未指定・不正・期限切れ・revoke 済み token は `401` になる。
- Admin は `POST /v2/admin/devices` に `{"name": "...", "device_type": "UNITY"}` を送り、発行時の `device_token` を一度だけ受け取る。token の plaintext/hash は後から取得できない。
- token の rotate は `POST /v2/admin/devices/{device_id}/token/rotate`。必要なら `overlap_seconds` を指定できる。revoke は `POST /v2/admin/devices/{device_id}/token/revoke` で、以後の request を拒否する。
- `CreateDeviceRequest` の `name` は 1〜255 文字、`token_ttl_seconds` は任意の 1 以上（既定 31,536,000 秒）。
- `admin-demo-token`、`intake-demo-token`、`worker-demo-token`、`v2-unity-device-token` は `YummyApiMock` のローカルテスト用であり、Unity build に埋め込まない。

## 1. 生成履歴一覧

### Request

```http
GET {base}/devices/unity/orders?state=COMPLETED&limit=50&cursor=<opaque-cursor>
Authorization: Bearer <unity-device-token>
Accept: application/json
```

OpenAPI の `servers.url` を使うと `{base}` は `https://<deployment-host>/v2` になる。実際の deployment host は未確定である。

| Query | 型・制約 | 意味 |
|---|---|---|
| `state` | `OrderState`, 任意 | 完全一致の state filter |
| `q` | string, 1〜100, 任意 | order ID または food name の case-insensitive substring |
| `food_name` | string, 1〜100, 任意 | food name の case-insensitive substring |
| `limit` | integer, 1〜100, default `50` | 1 page の件数 |
| `cursor` | string, 任意 | response の `next_cursor` をそのまま送る opaque signed cursor |

結果は `created_at DESC`、次に `order_id DESC` で並ぶ。cursor の中身を decode、編集、採番、再生成してはならない。filter を変えた cursor は `400` になる。

### `DeviceOrderListResponse`

```json
{
  "items": [/* CustomerOrderStatus */],
  "next_cursor": "<opaque-token>",
  "has_more": true
}
```

- `items`、`next_cursor`、`has_more` は必須。
- `next_cursor` は `string | null`。`has_more=false` のときは `null`。
- Device API は削除済み order を一覧へ戻さない。
- all-order scope なので、一覧には `DRAFT`、processing、review、failed、rejected、canceled も含まれ得る。食品選択候補は response の readiness を見て別途絞る。

## 2. order status

`GET /v2/devices/unity/orders/{order_id}` の response は `CustomerOrderStatus` である。

```json
{
  "order_id": "opaque-order-id",
  "food_name": "寿司",
  "state": "COMPLETED",
  "analysis": {"state": "COMPLETED"},
  "glb": {"state": "COMPLETED", "downloadable": true, "artifact_id": "opaque-glb-id"},
  "wav": {"state": "COMPLETED", "downloadable": true, "artifact_id": "opaque-wav-id"},
  "created_at": "2026-08-29T10:00:00Z",
  "updated_at": "2026-08-29T10:08:00Z"
}
```

| Schema | 必須フィールド | 制約・意味 |
|---|---|---|
| `CustomerOrderStatus` | `order_id`, `food_name`, `state`, `analysis`, `glb`, `wav`, `created_at`, `updated_at` | `food_name` は 1〜100 文字、日時は date-time |
| `CustomerStageStatus` | `state` | `StageState`。Unity projection で返るのは `analysis` のみ |
| `CustomerOutputStatus` | `state`, `downloadable` | `artifact_id` は `downloadable=true` のときだけ存在する |

重要な制約:

- Device の status projection は全 5 stage の配列を返さない。`analysis`、GLB、WAV の 3 projection だけで、moderation/retrieval/I23D の個別 state、review detail、failure detail は含まれない。
- `artifact_id` は selected・verified・completed output として server が download 可能と判定した場合だけ返る。`downloadable=false` のとき、欠落した `artifact_id` を推測してはならない。
- `CustomerOrderStatus` は `artifact_type`、`revision`、`sha256`、`media_type`、`size_bytes`、preview URL を返さない。Admin の詳細用 `ArtifactRevision` と混同しない。

Unity のメニュー候補は最低限、`state == COMPLETED`、`glb.downloadable == true`、`glb.artifact_id` が存在することを満たすものにする。WAV を必須表示・再生する場合は `wav.downloadable` も確認する。

## 3. selected artifact download

```http
GET /v2/devices/unity/orders/{order_id}/artifacts/{artifact_id}/download
Authorization: Bearer <unity-device-token>
Accept: model/gltf-binary, audio/wav
```

成功時は `200` で、content type は次のいずれかである。

| Artifact | Content-Type | 利用先 |
|---|---|---|
| `GLB` | `model/gltf-binary` | glTF loader |
| `WAV` | `audio/wav` | 食品ごとの咀嚼音 |

`Content-Disposition` に suggested filename が含まれる。次の場合は `404` であり、bytes を load しない。

- order が `COMPLETED` ではない
- `artifact_id` が order に属さない、または selected pointer と一致しない
- artifact type が GLB/WAV ではない
- `verified=false`
- artifact file が存在しない、または media type が不一致

この operation は normalized source image の preview を提供しない。`/v2/menu/{menu_item_id}/glb` 等の public sample を order artifact の代替にしてはならない。

### Unity が注意すべき integrity gap

規範 `ArtifactRevision` には `artifact_id`、`artifact_type`、`revision`、`sha256`、`verified` がある。しかし Unity Device の `CustomerOrderStatus` と artifact download response は `sha256`/`revision` を返さず、download response に checksum header も定義されていない。したがって現 contract のままでは、Unity が response bytes を server metadata と照合するための checksum を取得できない。

これは解消が必要な contract gap である。production implementation では、少なくとも次のいずれかを YummyService 側で追加してから、`FR31` の client-side SHA-256 gate を合格扱いにする。

- Unity 用 artifact metadata response に selected revision の `artifact_id/type/revision/sha256/verified/media_type/size_bytes` を追加する。
- download response の integrity header 等で同じ checksum を提供する。

`payload_sha256` は Hardware Payload の digest であり、GLB/WAV bytes の checksum ではない。

## 4. Unity Hardware Payload

### `GET /v2/devices/unity/orders/{order_id}/payload`

response は payload がまだ生成されていなければ `202`、生成済みなら `200` である。

#### `200 HardwarePayload`

| Field | 型 | 意味 |
|---|---|---|
| `order_id` | string | 対象 order |
| `payload_revision_id` | string | immutable payload revision identity |
| `revision` | integer >= 1 | payload revision number |
| `hardware_status` | `READY` / `UNSUPPORTED` / `AMBIGUOUS` / `UNSAFE` / `INVALIDATED` | hardware mapping 状態 |
| `device_type` | string | payload の対象 device type |
| `analysis_revision_id` | string/null | 参照した approved analysis revision |
| `profile_revision_id` | string/null | 参照した Food Profile revision |
| `control_values` | object<string, number>/null | `READY` のときだけ制御値。fail-closed 状態では null |
| `units` | object<string, string>/null | control value の単位 |
| `safety_constraints` | array<object>/null | 適用された安全制約 |
| `payload_sha256` | 64 桁 hex string | payload digest。GLB/WAV digest ではない |

OpenAPI schema には Admin 用 provenance の optional fields もあるが、Unity Device projection は上表の sanitized fields だけを返す前提で実装する。`READY` 以外で `control_values`、`units`、`safety_constraints` が存在しても使用せず、未知の status は fail closed にする。

payload が無い場合の body は次の固定形である。

```json
{
  "order_id": "opaque-order-id",
  "hardware_status": "NOT_READY",
  "control_values": null
}
```

このとき `Retry-After: 5` が返る。payload がある場合は `ETag: "<payload_sha256>"` が返り、次回 request に `If-None-Match` を付けて一致すれば `304`（body なし）になる。運用 kill switch `DELIVER_DEVICE_PAYLOADS` が無効な場合は `503` と `Retry-After: 5` である。

`UNSUPPORTED`、`AMBIGUOUS`、`UNSAFE`、`INVALIDATED` は control value を安全側に推測して補完する状態ではない。Unity は電気刺激・ハードウェア制御を開始せず、運用可能なエラー状態として扱う。

### `POST /v2/devices/unity/orders/{order_id}/payload/ack`

header と body:

```http
Idempotency-Key: <1..255 characters>
Content-Type: application/json
```

```json
{
  "payload_revision_id": "opaque-payload-revision-id",
  "success": true,
  "error_code": null,
  "applied_at": "2026-08-30T01:00:00Z"
}
```

- body 必須は `payload_revision_id` と `success`。
- `error_code` は任意の uppercase token（`^[A-Z][A-Z0-9_]{0,99}$`）。成功 ACK では null、失敗 ACK では必須。
- `applied_at` は任意の date-time。省略時は server 時刻。
- 新規 ACK は `201`、同じ Device identity と同じ `Idempotency-Key` の同じ request の replay は `200`。
- 同じ key を異なる body で再利用すると `409`。current payload revision と body の revision が違う場合も `404`/`409` として適用しない。
- `HardwarePayloadAck` の response は `ack_id`、`order_id`、`payload_revision_id`、`success`、`error_code`、`applied_at` が必須。`result` は `SUCCESS` または `FAILURE`。

ACK の idempotency key は Device 単位で管理される。Unity 側では payload revision と適用処理の request identity を保存し、timeout 後は同じ key/body で再送する。

## 5. Public menu と Unity Device API の違い

`/v2/menu` は認証不要の published menu read model で、branch の開発用 UI/サンプル用途である。

```json
{
  "id": "sushi",
  "display_name": "寿司",
  "description": "握り寿司の3D・音声サンプルです。",
  "available": true,
  "sample_glb_url": "/v2/menu/sushi/glb",
  "sample_wav_url": "/v2/menu/sushi/wav"
}
```

`PublicMenuItem` の必須 field は `id`、`display_name`、`description`、`available`、`sample_glb_url`、`sample_wav_url`。最後の 2 つは `string | null` である。公開 item は `published=true` のものだけだが、`available=false` の item が一覧に残る場合はある。

Unity 側でこの public sample を意図的に使う場合の routes は次の通り。

- `GET /v2/menu` — `PublicMenuResponse { items: PublicMenuItem[] }`
- `GET /v2/menu/{menu_item_id}` — `PublicMenuItem`
- `GET /v2/menu/{menu_item_id}/glb` — `model/gltf-binary` bytes
- `GET /v2/menu/{menu_item_id}/wav` — `audio/wav` bytes

この schema に `thumbnail_url`、`sample_audio_url`、`audio_url` は存在しない。preview 画像 URL も存在しない。`sample_glb_url`/`sample_wav_url` は generated order の selected artifact reference ではない。

## Domain schema と workflow

### Enum

| Schema | wire value |
|---|---|
| `OrderState` | `DRAFT`, `QUEUED`, `PROCESSING`, `AWAITING_ADMIN_REVIEW`, `COMPLETED`, `REJECTED`, `FAILED`, `CANCELED` |
| `Stage` | `INPUT_MODERATION`, `EXAMPLE_RETRIEVAL`, `FOOD_ANALYSIS`, `IMAGE_TO_3D`, `AUDIO_GENERATION` |
| `StageState` | `PENDING`, `QUEUED`, `PROCESSING`, `COMPLETED`, `COMPLETED_WITH_WARNING`, `FAILED`, `CANCELED` |
| `ArtifactType` | `SOURCE_IMAGE_ORIGINAL`, `SOURCE_IMAGE_NORMALIZED`, `FOOD_ANALYSIS_JSON`, `GLB`, `WAV` |
| `ModerationDecision` | `PASS`, `REVIEW`, `BLOCK` |
| `FoodAnalysisStatus` | `VALID`, `REVIEW_REQUIRED` |
| `AnalysisDecision` | `APPROVED`, `REVIEW` |

Workflow は次の DAG である。

```text
INPUT_MODERATION
|-- EXAMPLE_RETRIEVAL -- FOOD_ANALYSIS -- AUDIO_GENERATION
`-- IMAGE_TO_3D
```

- `REVIEW` は `BLOCK` と異なる。moderation review は downstream を止めるが、Food Analysis review は analysis/audio branch だけを止め、I23D は進行し得る。
- retrieval exhaustion は許可された `COMPLETED_WITH_WARNING` / Zero Shot outcome であり、任意の失敗を warning に変換してよいわけではない。
- `AUDIO_GENERATION` は特定の Food Analysis revision が Admin `APPROVED` であることを必要とする。
- `COMPLETED` は moderation 承認、retrieval の `COMPLETED` または許可された warning、approved analysis、verified `FOOD_ANALYSIS_JSON`、verified selected GLB、verified selected WAV がすべて成立した状態である。

### `ArtifactRevision`

規範 schema の必須 field は次の 5 つである。

| Field | 制約 |
|---|---|
| `artifact_id` | non-empty opaque string |
| `artifact_type` | `ArtifactType` |
| `revision` | non-empty `RevisionId` |
| `sha256` | `[A-Fa-f0-9]{64}` |
| `verified` | boolean |

optional field として `analysis_revision` 等の provenance、`filename`、`media_type`、`size_bytes`、`created_at`、`selected` がある。revision は immutable で、latest/current/selected pointer は別概念である。Unity は filename、QR GUID、order 名だけを cache key にせず、利用可能な場合は `artifact_id + revision + sha256` を cache identity にする。

### Food Analysis

`FoodAnalysis` は food characteristics のための schema であり、hardware/electrical control value を含めない。

- 必須: `schema_version`、`analysis_revision`、`input_food_name`、`normalized_food_name`、`english_food_name`、`visual_description`、`audio_prompt`、`texture`、`status`、`confidence`、`warnings`、`provenance`
- `texture`: `chewiness`、`firmness`、`elasticity`、`adhesiveness`、`brittleness`、`moisture` の 6 軸。各 integer 1〜10。
- `attributes` は `ingredients`、`cooking_method`、`serving_state` の構造化 extension。任意の top-level key を追加しない。
- `confidence` は informational only。gameplay、haptic、electrical control の threshold を自動導出しない。

### Unity Device response には直接返らない domain schema

Unity が将来 order intake や Admin projection と接続する場合に備え、関連 schema の制約も記録する。現行の Unity Device status はこれらをそのまま返さない。

| Schema | 必須/制約 |
|---|---|
| `Sha256` | 64桁の `[A-Fa-f0-9]` hexadecimal |
| `RevisionId` | 1文字以上の opaque string |
| `SourceImage` | `media_type` 必須。`image/png`/`image/jpeg`/`image/webp`、animated WebP不可。raw最大20 MiB、normalized最大5 MiB・1,500,000 pixels、aspect ratio 0.25〜4、拡大なし |
| `OrderInput` | `food_name` 必須（1〜100文字）、`image` は `SourceImage|null`（DRAFT中だけ null/省略可）、`note` 最大500文字 |
| `AnalysisProvenance` | `model_revision`、`prompt_revision`、`schema_revision` が必須の `RevisionId` |
| `StageStatus` | `stage`（`Stage`）と `state`（`StageState`）が必須。`started_at`/`completed_at` は任意 date-time |

`SourceImage` の normalization は EXIF 除去、sRGB/RGB 化、透明部分の白合成、拡大なしの縮小を行う。これらは入力/worker/Admin 側の contract であり、Unity Device の preview download が存在することを意味しない。

## Error / compatibility rules

エラー payload は `Content-Type: application/problem+json` の `ProblemDetails` を使う。

```json
{
  "type": "about:blank",
  "title": "Artifact not found",
  "status": 404,
  "detail": "...",
  "instance": "/v2/devices/unity/orders/..."
}
```

`type`、`title`、`status` が必須。`status` は 400〜599、`detail`/`instance` は任意、unknown extension field は許容される。Unity は `401`、`403`、`404`、`409`、`429`、`5xx`、timeout/cancellation を一つの成功/一般エラーへ潰さず、operation ごとの retry 方針を持つ。未知の enum、必須 field 欠落、`verified=false`、wrong type、checksum 不一致は fail closed にする。

## 現行 Unity 実装との差分

この repository の現行 network adapter は、今回の v2 Device API ではなく、開発用 Admin menu/sample API を参照している。後続 implementation の着手時に次を修正する。

- `NetworkFoodCatalogSource` は `/v2/admin/menu` と固定の `admin-demo-token` を使用している。Unity production は `/v2/devices/unity/orders` と `UNITY` Device token を使用する。
- `NetworkFoodCatalogSource` の `MenuResponseDto` には `thumbnail_url`、`sample_audio_url`、`audio_url` があるが、今回の `PublicMenuItem` で規範化されている音声 field は `sample_wav_url` のみである。generated order の status には preview URL 自体がない。
- `NetworkFoodLoader` は menu の URL を直接取得するが、selected `order_id`/`artifact_id`、revision、server checksum を保持しない。formal route と immutable artifact identity へ置き換える必要がある。
- `NetworkConnectionTester` も Admin menu と固定 token を用いるため、production の Device token/configuration/compatibility check と一致しない。
- `YummyServiceV2Contract` の local constant はこの snapshot の commit/checksum より古い。コードを v2 Device API へ移行する際に、本ドキュメントの snapshot と同時に更新する。

Standalone local catalog は API fallback ではなく独立 source として維持する。API が未接続・認証失敗・contract gap の場合でも、local item を消したり v1 route へ fallback したりしない。

## 実装時の順序

1. Unity Device token を secret 配布経路から受け取り、build source/log に漏らさない。
2. `/devices/unity/orders` の `DeviceOrderListResponse` を transport DTO として受け、opaque cursor を保存・再送する。
3. `CustomerOrderStatus` を strict enum mapping し、`COMPLETED + glb.downloadable + glb.artifact_id` だけを generated food candidate にする。
4. preview が必要な場合は、現在の Device API に normalized image operation がないことを UI/要件へ反映し、public sample URL を preview と誤認しない。
5. selected GLB/WAV は device artifact route から個別取得する。全 item の GLB を一覧時に先読みしない。
6. artifact checksum の contract gap が解消されるまでは、SHA-256 検証を完了した production integration と判定しない。
7. Payload は `READY` かつ必要な値が揃う場合だけ適用し、`ETag`/`Retry-After`/ACK idempotency を実装する。

検証の参照先は YummyService の [`YummyApiMock/tests/test_v2_unity.py`](https://github.com/YummyVerseVR/YummyService/blob/ru322/main/YummyApiMock/tests/test_v2_unity.py) と [`YummyOrderServer/tests/test_v2_device_api.py`](https://github.com/YummyVerseVR/YummyService/blob/ru322/main/YummyOrderServer/tests/test_v2_device_api.py) である。これらは一覧 filter/cursor、role boundary、payload 202/200/304、fail-closed status、ACK replay/conflict、selected verified artifact download を確認している。
