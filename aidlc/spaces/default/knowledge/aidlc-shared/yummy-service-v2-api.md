# YummyService v2 API Contract Knowledge

## Authority Snapshot

- Repository: `https://github.com/YummyVerseVR/YummyService`
- Branch reviewed: `main`
- Commit reviewed: `546b455fedd205fb686ca7b93d6af596bced7879`
- Reviewed at: 2026-08-24
- Normative v2 files:
  - `contracts/v2/openapi.yaml` — SHA-256 `73a21a380d23a136f92ddea7bc45cfcc9556aac040f5aa9a9e1c58d34ac0f5f0`
  - `contracts/v2/README.md` — SHA-256 `e3f6635bf215b2e96b6005d8946fe5c6b0549f8db995efa11b2ac9139d91e46a`
- OpenAPI metadata: `openapi: 3.1.0`、`info.version: 2.0.0-draft`。
- `feature/v2-analysis-review-timing-persistence` との差分確認時、`contracts/v2/` の規範ファイルに差分はなかった。

この snapshot はレビュー時点の外部契約を `aidlc` 内で追跡するためのもの。YummyService 側の新しい commit を自動的に採用せず、契約 diff と本 intent の requirement/adapter 影響を review してから更新する。

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
- Moderation の `REVIEW` は曖昧、`BLOCK` は明示拒否であり同一に扱わない。
- `EXAMPLE_RETRIEVAL` と `IMAGE_TO_3D` は moderation 後の独立 sibling である。
- Retrieval exhaustion は Zero Shot の `COMPLETED_WITH_WARNING` として analysis を継続できる。一般的な failure fallback ではない。
- Food Analysis review は analysis/audio branch だけを保留し、独立した `IMAGE_TO_3D` は継続し得る。

### States

- Stage: `PENDING`, `QUEUED`, `PROCESSING`, `COMPLETED`, `COMPLETED_WITH_WARNING`, `FAILED`, `CANCELED`。
- Order: `DRAFT`, `QUEUED`, `PROCESSING`, `AWAITING_ADMIN_REVIEW`, `COMPLETED`, `REJECTED`, `FAILED`, `CANCELED`。
- Moderation: `PASS`, `REVIEW`, `BLOCK`。
- Food Analysis schema/rule status: `VALID`, `REVIEW_REQUIRED`。
- Food Analysis Admin decision: `APPROVED`, `REVIEW`。

`AWAITING_ADMIN_REVIEW` は order 全 branch の停止を意味しない。UI は order state と各 stage state を別々に表現する。

### Completion Gate

Order が `COMPLETED` になるには、次がすべて必要である。

1. Moderation が承認済み。
2. Example Retrieval が `COMPLETED` または `COMPLETED_WITH_WARNING`。
3. Admin `APPROVED` の Food Analysis revision と、verified immutable JSON artifact が存在する。
4. `IMAGE_TO_3D` の verified GLB が存在する。
5. `AUDIO_GENERATION` の verified WAV が存在する。

GLB または WAV の最終失敗は、valid Food Analysis/Hardware data を保持しても order 全体の `COMPLETED` を妨げる。

### Artifact Contract

- Artifact types: `SOURCE_IMAGE_ORIGINAL`, `SOURCE_IMAGE_NORMALIZED`, `FOOD_ANALYSIS_JSON`, `GLB`, `WAV`。
- `ArtifactRevision` required fields: `artifact_id`, `artifact_type`, `revision`, `sha256`, `verified`。
- `sha256` は64桁の hexadecimal string。
- Artifact revision は immutable で in-place update しない。latest/current/selected は別 pointer で表す。
- `analysis_revision` は該当 artifact に任意で関連付く。
- Unity/Viewer は mutable filename や旧 food GUID ではなく、order/item identity と selected immutable artifact revision を扱う。

### Source Image and Food Analysis Schemas

- `OrderInput.food_name`: 1〜100文字。
- `OrderInput.image.media_type`: `image/png`, `image/jpeg`, `image/webp`。animated WebP は不可。
- Raw image: 最大20 MiB。
- Normalized image: 最大5 MiB、最大1,500,000 pixels、aspect ratio 1:4〜4:1、拡大なし。EXIF 除去、sRGB/RGB、transparent pixel は白へ合成する設計だが normalization 実装は deferred。
- `OrderInput.note`: 任意、最大500文字。
- Food texture axes: chewiness、firmness、elasticity、adhesiveness、brittleness、moisture。各1〜10で、大きいほど特性が強い。
- Food Analysis は food characteristics だけを持ち、muscle/electrical stimulation value を含めない。
- `confidence` は情報表示専用で、stable scale/range は未確定。自動制御 threshold に使ってはならない。
- Food Analysis は `additionalProperties: false`。任意 top-level LLM key は禁止され、extension は構造化された `attributes` に限定される。

### Problem Schema

`application/problem+json` の `ProblemDetails` は `type`, `title`, `status` を必須とし、`detail`, `instance` を任意で持つ。`status` は400〜599。`additionalProperties: true` なので client は未知 extension を理由に response 全体を拒否しない。

## Transport and Runtime Contract Status

現行 `contracts/v2/openapi.yaml` は domain schema skeleton であり、呼出可能な HTTP API ではない。

- `paths: {}` — endpoint/path/method/request/response が0件。
- Server URL: `https://vps.example.invalid/v2` — deployment URL ではない。
- `security: []` — anonymous production access の決定ではない。v2 README は authentication を deferred と明記する。
- Deferred: HTTP handlers、authentication、artifact lookup/download、customer token、device token、events/intake session、UI、TLS、retry timer、measured SLO、physical artifact storage behavior 等。
- `YummyOrderServer` と `YummyApiMock` に確認できる route は `/v1/...` だけで、v2 HTTP route は存在しない。

したがって Unity/Physical Viewer の v2 adapter は、domain mapping と test fixture までは設計できるが、本番 HTTP request path を実装・合格判定できない。v1 endpoint または旧 `/{guid}/model` を v2 と見なして fallback してはならない。

## V1 Retirement Policy

- **YummyVerseUnity から利用する v1 API は廃止済みであり、金輪際使用しない。**
- Production、development、test、demo、障害時 fallback、migration compatibility、Standalone の代替を含む、あらゆる runtime から `/v1/...` へ outbound request を送らない。
- v1 client、DTO、endpoint configuration、server mock を新しい runtime dependency として追加・維持しない。
- 唯一許容する v1 形式の data は、Contract Guard が v1 URL/response を拒否することを証明する local negative test fixture である。Fixture は v1 server へ接続しない。
- External YummyService repository に v1 contract/route が履歴として残っていても、YummyVerseUnity の利用許可を意味しない。

## Required Consumer API Surface Before Construction

YummyVerseUnity と Physical Viewer が current requirements を満たすには、YummyService v2 側で少なくとも次の transport contract が必要である。path 名は未確定のため、この文書では capability として記録する。

1. **History query**: 利用者に公開可能な order/item を cursor pagination、安定 sort、state/filter とともに列挙する。
2. **Order detail/status**: order state、5 stage state、warning/review/failure の区別、表示名に使える customer-visible metadata を返す。
3. **Selected artifact metadata**: order が選択する normalized source image、GLB、必要なら WAV の `artifact_id/type/revision/sha256/verified` を返す。
4. **Artifact download**: authorized client が immutable preview image/GLB を binary download でき、media type、size、integrity、redirect/range behavior が定義される。
5. **Authorization**: Quest と iPad 等の viewer に適した token scope、lifetime、refresh/revocation、order/history visibility を定義する。admin/worker credential を client に配布しない。
6. **Change detection**: polling interval/conditional request、ETag/Last-Modified、event delivery のいずれかと rate limit/retry guidance を定義する。
7. **Problems**: 各 operation の ProblemDetails、401/403/404/409/429/5xx、retryable/non-retryable の意味を定義する。
8. **Compatibility**: deployed contract version/revision を識別でき、v2-incompatible server を fail closed で検出できる。

## Current Unity Client Gap

- `FoodDownloader` は `baseEndPointUrl + {guid} + "/model"` を `GET` し、10秒 timeout で直接 GLB を取得する。これは v2 contract に存在しない。
- `FoodContext` は QR GUID 変更を model download trigger にする。現行 product requirement と v2 order/artifact identity の両方に不適合。
- `IFoodFetchable.Download(Guid)` と `FoodDownloadResult.RequestedGuid` は v2 order/item/artifact revision を表現できない。
- `FoodDownloader` は response bytes を一度 base64 encode/decode し、固定名 `test.glb` へ保存する。immutable artifact cache identity と並行 request 安全性を満たさない。
- 現行 default endpoint は旧 Yummy Control Server URL であり、v2 contract URL/compatibility を検証しない。

これらは確認済み gap であり、この文書更新だけでは code が v2 対応したことにならない。
