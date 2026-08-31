# Source Migration Map

## Purpose

この文書は、移管元の `docs` が削除されても「何が書かれており、どの恒久 requirement ID へ移したか」を判断できるようにする。元ファイルへのリンクは provenance であり、要件理解の前提ではない。

## `tutorial-requirement.md` Migration

移管元には同じ仕様が途中から重複して連結されていた。重複は新しい要求として数えず、次の一つの仕様へ正規化した。

| Source topic | Self-contained requirement summary | Destination |
|---|---|---|
| Purpose | 展示型 YummyVerse の tutorial core を data-driven step sequence とし、QR/scoop/clear の game feature は event 経由で観測する | `FR2`〜`FR4`, `FR23` |
| Step normalization | 全 step は `Present → Await → Succeed`。説明だけの状態を別型にしない | `FR3` |
| Layer separation | AppState、TutorialSequence、Presenter、Game を分離する | `FR1`, `FR4` |
| Dependency direction | Game→EventBus→TutorialRunner→Presenter の一方向。Tutorial 中も Game を稼働し、終了時に再起動しない | `FR2`, `FR4`, `NFR2` |
| Runtime environment | Unity 6 Awaitable または UniTask、async/await、CancellationToken。巨大 enum/switch を禁止 | `FR5`, `NFR1`, Constraints |
| AppState | Attract/Tutorial/FreePlay/Outro の4状態。step state を混ぜない | `FR1` |
| Game event contract | Start、QR/anchor detection/lost、FoodScooped、DishCleared、MenuItemSelected、UserAbsent | `FR4`, `FR8`, `FR19`, `FR22`, `FR23` |
| Completion conditions | Button、Time、GameEvent、CountedEvent、AnyOf を再利用可能な SO として用意 | `FR5` |
| Step types | Narration/Task/Choice の3種類だけ。Task に hint/rescue/success を統合 | `FR6`, `FR7` |
| Sequence/context/presenters | Sequence は list、Choice は sub-sequence、context blackboard、UI は Presenter interface 越し | `FR4`, `FR6`, `FR10` |
| Runner | 順次実行、repeat skip、step enter/exit log、current ID、exception propagation | `FR3`, `FR9` |
| Abort/reset | UserAbsent、idle、staff reset、cancel 一括伝播、finally reset、配置は保持 | `FR8`, `FR24`, `NFR3`, `NFR4` |
| Rescue policy | 5秒 hint、30秒 rescue、AutoAdvance/ForceComplete/ReturnToAttract、analytics | `FR7`, `NFR7`, `NFR8` |
| S1〜S19 | Start、紹介、QR、前菜、scoop、clear、menu、selected food、outro。S15以降は FreePlay | `FR11`, Canonical Experience Flow |
| Acceptance | Asset 編集性、3秒 reset、無限待機防止、10 session、seamless transition、debug HUD | `FR3`, `FR7`〜`FR10`, `NFR2`〜`NFR4`, AC5〜AC7 |
| Anti-patterns | 巨大 FSM、message state、Game 直接参照、任意 jump、別 scene、timeout 無しを禁止 | Constraints |

### QR-related normalization

移管元の初期版は QR を食品 GUID 選択にも用いていた。食品 identity は Virtual Menu の item ID から得る。S3/event は一度「モデルの出現 anchor designation」として移管したが、2026-08-24 の追加指示により S3 の案内・待機自体を現行 TutorialSequence から削除した。Anchor designation の製品責務は `FR16`, `FR17`, `ADR-003` に残る。

## `tutorial-usage.md` Migration

| Source topic | Self-contained requirement/constraint summary | Destination |
|---|---|---|
| Overall wiring | Event publisher と low-level input/QR が EventBus へ入り、Condition が購読。Step からの依頼は CommandBus/Router | `FR4`, Components |
| Existing implementation map | AppStateMachine、SessionController、TutorialRunner、SO steps/conditions、Presenters、existing game services | Components: Existing Implementation Baseline、shared `tutorial-system.md` |
| Localization/assets | 日本語 locale、default asset の冪等生成、既存文言を上書きしない | `FR10` |
| Input/scene setup | Start と StaffReset、Restaurant 同一 scene、scene DI scope、UI/feedback/debug components | `FR1`, `FR8`〜`FR10`, shared `tutorial-system.md` |
| Anchor setup | QR Transform へ model を追従させず、保存 placement を使用。復元失敗は再設定 | `FR16`, `FR17`, shared project context |
| Event publishing | Start、QR、Menu selection、UserAbsent の自動経路。FoodScooped/DishCleared は game 実装が発行 | `FR4`, `FR19`, `FR22`, `FR23` |
| Game commands | Serve appetizer、destroy food、show/hide menu は router 境界で委譲 | `FR4`, `FR11`, `FR24`, Components |
| Routine editing | 文言、待ち時間、hint/rescue、sequence、idle timeout、Attract message を data asset で編集 | `FR3`, `FR7`, `FR10` |
| Shared SO safety | Condition/Step asset に runtime state を保持しない | `FR5`, Constraints |
| Reset ownership | GameResetter が food/menu/download/QR/idle を reset。Presenter/context/session selection も reset。Anchor は保持 | `FR8`, `FR24` |
| Debug/test | HUD/log prefix、dummy events、QR fake、Editor smoke flow、F5 reset | `FR9`, Acceptance Scenarios |
| Intentional implementation differences | Runner/Session は POCO、Presenter は reactive state、Voice/Choice Presenter 追加、S1 は Attract config | Components、shared `tutorial-system.md` |
| Anti-patterns | Direct Game reference、new step enum、jump、separate scene、no timeout、SO runtime fields を禁止 | Constraints |

## 2026-08-24 Additional Requirement Migration

| User requirement | Destination |
|---|---|
| VR start button begins an interactive tutorial | `FR1`, `FR2` |
| First use orthodox food such as an apple, then serve user-ordered food | `FR11`, Canonical Experience Flow, `AC1` |
| Stop using QR to generate/show a model because loading is slow | `FR13`, `FR16`, `ADR-003` |
| QR only specifies the model appearance anchor | `FR16`, `FR17`, `Q5` |
| Select generated model data from a VR history menu and call it immediately | `FR12`, `FR13`, `Q1` |
| Preload image previews instead of 3D models | `FR14`, `NFR5`, `ADR-002` |
| Provide a physical menu/viewer on iPad etc. | `FR15`, `Q2` |
| Add spoon reaction with a farthest-two-points-based transparent AABB | `FR18`, `FR19`, `Q3` |
| Controller vibration would be nice | `FR20` (`SHOULD`), `Q4` |
| Shrink food progressively, scatter crumbs, finally disappear | `FR21`, `FR22`, `ADR-004` |
| Do not implement complex cross sections | `NFR9`, Constraints, Out of Scope |
| Requirements must remain understandable after `docs` deletion | `AC9`, project memory, shared knowledge |

## Coverage Result

- Unmapped normative requirement: None.
- Pure editor操作の細目は shared `tutorial-system.md` に現行 baseline として保存し、製品 requirement ID を水増ししていない。
- 解釈を必要とする項目は `Q1`〜`Q11` に残し、確定済み要件として補完していない。

## YummyService v2 Contract Mapping

| Contract topic | Self-contained contract fact | Destination |
|---|---|---|
| Version/authority | OpenAPI 3.1、`2.0.0-draft`、YummyService `ru322/main@97c9ed7...`、104 paths/124 schemas | `FR25`, `NFR10`, contract summary |
| HTTP readiness | Unity Device の history/status/artifact/payload/ACK paths と schemas、device bearer auth が定義済み。server は `.invalid` placeholder | `FR25`, `AC15`, `Q6`〜`Q10` |
| Unity history/status | `GET /devices/unity/orders` → `DeviceOrderListResponse`、`GET /devices/unity/orders/{order_id}` → `CustomerOrderStatus`。Device status は sanitized projection | `FR27`, `FR28`, `AC13`, `Q6`, `Q9` |
| Unity artifact delivery | selected completed verified GLB/WAV の `/artifacts/{artifact_id}/download`。media type/Content-Disposition は定義済み、checksum/revision metadata は Unity response にない | `FR29`〜`FR31`, `AC12`, `Q10` |
| Unity payload | `/payload` の `200/202/304/503`、ETag/Retry-After、`/payload/ack` の Idempotency-Key/201/200/409 | `FR25`, `FR33`, contract summary |
| Device authentication | `deviceBearerAuth` と `UNITY` device token の issue/rotate/revoke。Mock static token は dev only | `FR25`, `FR32`, `Q7` |
| Public menu boundary | `/menu` は published sample の `PublicMenuItem` と GLB/WAV。generated order history/artifact/preview の代替ではない | `FR13`, `FR27`, `FR30`, `SRC-6` |
| Workflow DAG | Moderation→Retrieval→Analysis→Audio と moderation→I23D。I23D は analysis/audio branch から独立 | `FR26`, contract summary |
| Order/Stage states | 8 OrderState、7 StageState、review/warning semantics | `FR26`, `FR28`, `AC11` |
| Completion | Approved moderation/analysis、retrieval success/warning、verified JSON/GLB/WAV が必要 | `FR28`, `FR29`, `Q11` |
| Artifact immutability | Artifact ID/type/revision/SHA-256/verified。current selection は別 pointer | `FR29`, `FR31`, `ADR-007` |
| Preview/model types | `SOURCE_IMAGE_NORMALIZED` と `GLB` は別 immutable artifact type | `FR29`, `FR30` |
| ProblemDetails | `type/title/status` required、extension allowed | `FR33` |
| Security | Unity Device route は `deviceBearerAuth`。Customer/Viewer scope と deployed secret delivery は未確定 | `FR32`, `NFR13`, `Q7` |
| Food Analysis | Food properties only、confidence は制御 threshold に使用不可 | Constraints、shared API knowledge |
| Current Unity gap | `/v2/admin/menu`、固定 `admin-demo-token`、menu URL download、artifact identity/checksum 非保持 | `FR25`, `FR29`〜`FR31`, `NFR14`, `ADR-006`/`ADR-007` |
| V1 retirement | YummyVerseUnity では v1 API を廃止し、全 runtime/environment/fallback から恒久的に利用禁止 | `FR25`, project memory, shared API knowledge |
| Standalone continuity | Standalone Mode は API 非依存の端末内食品 source として継続する | `FR35`, `ADR-008`, project memory |
| Unified post-tutorial menu | S14 後の一つの UI に v2 API 食品と Standalone 食品を同時表示し、source に応じて load する | `FR34`, S16/S17, `AC16`, `ADR-008` |

- YummyService v2 の unmapped normative domain rule: None for the current read-only catalog/model-consumer scope. Transport の未達は preview、全 stage/status detail、Unity artifact checksum、deployment/compatibility policy として明示済み。
- Unity から order intake/upload/submit、Admin/worker operation を行う要求は現 scope にないため Out of Scope とした。
