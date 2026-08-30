# Requirements

## Intent Analysis

- 目的: 来場者が説明に沿って基本的な食品で操作を学び、生成済み食品をメニューから選び、安定したアンカー位置ですくって食べられる展示体験を定義する。
- 対象ユーザー: 来場者、展示運営者、iPad 等を使う外部閲覧者、開発者。
- 成功指標: `docs` 非依存の要件追跡、シームレスな Tutorial→FreePlay、QR 待ちのない食品選択、軽量なメニュー表示、簡易で理解しやすい食事リアクション、中断後の確実なリセット、YummyService v2 の order/artifact 契約に準拠した安全な API 利用。
- 優先規則: 本 intent の QR/食品選択要件は `260821-spatial-anchor-food-placement` の QR GUID 選択要件を supersede する。

## Functional Requirements

### Tutorial and Session

- **FR1: VR 空間のスタート操作からチュートリアルを開始できること** (`MUST`)
  - 来場者が VR 空間内のスタートボタンを押すと `Attract` から `Tutorial` へ遷移する。
  - アプリ全体の粗い状態は `Attract`、`Tutorial`、`FreePlay`、`Outro` の4状態とし、チュートリアル内部のステップを AppState に追加しない。
  - 検証条件: Attract 中の一度の有効なスタート操作で Tutorial が一度だけ開始し、重複セッションを生成しない。
  - 出典: 2026-08-24 利用者要求、移管元チュートリアル仕様。

- **FR2: 説明と実操作を交互に進めるガイド付き体験であること** (`MUST`)
  - 各操作課題では説明またはデモを提示したまま、実際のゲーム機能を動かして達成できる。
  - チュートリアル中もゲーム機能を稼働させ、終了時にゲームの再起動、シーン再読込、別ゲームモードの初期化を必要としない。
  - 検証条件: 来場者が提示された説明に従い、すくい、完食を実ゲームイベントで達成できる。Anchor designation はチュートリアルの案内・待機ステップに含めない。
  - 出典: 2026-08-24 利用者要求、移管元チュートリアル仕様。

- **FR3: チュートリアルをデータ駆動のステップ列として定義すること** (`MUST`)
  - 1ステップを `Present → Await → Succeed` として正規化する。
  - ステップ列、文言、待機秒数、順序、繰り返し時のスキップ可否を ScriptableObject から編集できるようにする。
  - 説明だけの場面も独立した AppState ではなく、ボタン入力または時間経過を完了条件に持つステップとして扱う。
  - 検証条件: コードを変更せずに文言・秒数・順序を差し替え、変更後のシーケンスを実行できる。
  - 出典: 移管元チュートリアル仕様 §1.1、§3、§6、§7。

- **FR4: チュートリアルとゲーム/UI の責務を一方向の境界で分離すること** (`MUST`)
  - ゲーム機能は `IGameEventBus`/publisher へ事実を発行し、TutorialCondition が購読する。
  - TutorialRunner と TutorialStep はゲームの具象コンポーネントを直接呼ばない。食品提供、全食品破棄、メニュー表示/非表示などの依頼は `IGameCommandBus` と一つの router 境界を介する。
  - 表示・音声・ヒント・成功演出・選択 UI は Presenter interface を介し、Step が View の具象を直接操作しない。
  - ゲーム側はチュートリアル専用の状態や都合を知る必要がない。
  - 検証条件: Tutorial assembly/domain から Game/View の具象参照がなく、イベント購読と command 発行だけで通しフローを実行できる。
  - 出典: 移管元チュートリアル仕様 §1.2〜§1.3、§3.2、§3.7、移管元利用ガイド §0、§2。

- **FR5: 再利用可能な完了条件を組み合わせられること** (`MUST`)
  - 最低限、ボタン押下、時間経過、単一ゲームイベント、指定回数イベント、子条件のいずれかを待つ条件を提供する。
  - `AnyOfCondition` により「ボタンまたは時間経過」を新しい専用型なしで表現できる。
  - 全待機処理は呼出元の `CancellationToken` を受け取り、キャンセル時に完了扱いにしない。
  - ScriptableObject の Condition にセッション中の進捗を保持せず、実行状態はローカルに閉じる。
  - 検証条件: 同一 Condition asset を複数ステップ/セッションで共有しても、前回実行の状態が次回へ漏れない。
  - 出典: 移管元チュートリアル仕様 §2、§3.3、移管元利用ガイド §3.3。

- **FR6: Step の具象を Narration、Task、Choice の3種類に限定すること** (`MUST`)
  - `NarrationStep`: message/任意音声を提示し、完了条件を待つ。
  - `TaskStep`: instruction を提示し、達成待ち、ヒント、救済、成功 feedback を一つのステップで扱う。
  - `ChoiceStep`: prompt/options を提示し、選択または timeout 時の既定値を context に保存する。
  - Task の催促を別ステップにせず、`OK!` 等も独立ステップではなく共通 success feedback asset とする。
  - 分岐は Choice 結果による sub-sequence 選択で表し、任意の step jump を持たせない。
  - 検証条件: 既定シーケンスが3種類だけで表現され、ステップ型ごとの巨大 enum/switch が存在しない。
  - 出典: 移管元チュートリアル仕様 §3.4〜§3.6、§6、§8。

- **FR7: 操作の停滞に対してヒントと救済を行うこと** (`MUST`)
  - TaskStep は既定5秒の `hintDelaySeconds` 後にテキスト強調、デモ動画、または両方を提示できる。
  - 既定30秒の `rescueTimeoutSeconds` 後に `AutoAdvance`、`ForceComplete`、`ReturnToAttract` のいずれかを実行する。
  - 既定 policy は `AutoAdvance` とし、timeout のない TaskStep を許可しない。
  - ヒント提示と救済の発生を step ID、経過時間、policy とともに analytics へ記録する。
  - 検証条件: 全 TaskStep で無操作時にヒント→救済の順に進み、無限待機しない。
  - 出典: 移管元チュートリアル仕様 §3.4、§5、§7、移管元利用ガイド §3.5。

- **FR8: セッションをどのステップからでも一括中断・初期化できること** (`MUST`)
  - `OnUserAbsent`、グローバル無操作 timeout、スタッフ用 reset 入力、`ReturnToAttract` を中断トリガーとする。
  - セッション単位の `CancellationToken` を全非同期処理へ伝播し、step ごとの Attract 復帰線を作らない。
  - `finally` 相当の経路で、食品、注文内容、メニュー UI、ダウンロード context、QR 認識状態、無操作監視、Presenter、TutorialContext の一時状態を初期化する。
  - 永続化した Spatial Anchor/food placement、生成履歴など来場者セッション外の設定・資産は通常 reset で削除しない。
  - reset 自体は中断に用いた token に巻き込まれず、完了まで実行する。
  - 検証条件: 任意のステップから reset して3秒以内に Attract へ戻り、一時状態が次の来場者へ残らない。
  - 出典: 移管元チュートリアル仕様 §5、§7、移管元利用ガイド §4。

- **FR9: 現場でチュートリアル進行を診断できること** (`MUST`)
  - Runner は各 step の enter/exit を step ID と経過時間付きで記録し、例外を上位の SessionController へ伝播する。
  - 現在 step ID を公開し、AppState、step ID、経過秒を debug HUD に表示する。
  - debug HUD は Editor/Development Build で有効、本番では無効化または明示的に強制有効化できる。
  - テスト用にすくい、完食、UserAbsent、QR/anchor designation を手動発火できる代替入力を用意できる。
  - 検証条件: 正常、救済、中断、例外の各経路をログと HUD から識別できる。
  - 出典: 移管元チュートリアル仕様 §4、§5、§7、移管元利用ガイド §5。

- **FR10: チュートリアル表示をローカライズ可能なデータとして保守できること** (`MUST`)
  - message、instruction、choice、hint は `LocalizedString` または同等の localization asset を参照する。
  - デフォルト asset の生成は冪等で、既存 asset と入力済み文言を無断で上書きしない。
  - Presenter はシーンの DI scope から解決可能な位置に置き、チュートリアル専用 scene を作らない。
  - 検証条件: 日本語 locale で既定文言を表示でき、再生成操作後も編集済み asset が保持される。
  - 出典: 移管元チュートリアル仕様 §3、移管元利用ガイド §1、§3.1。

- **FR11: 基本食品から利用者選択の生成食品へ進むこと** (`MUST`)
  - 初回体験ではリンゴなど認知しやすいオーソドックスな食品を前菜として提供する。
  - 前菜で少なくとも「すくう」と「完食する」を体験した後、利用者へ生成済み食品の選択を促す。
  - Tutorial の境界は前菜操作の説明完了時とし、生成食品の選択・提供・完食・Outro は同じ session の FreePlay で扱う。
  - 繰り返し利用者には設定されたチュートリアル step をスキップできるが、食品選択と FreePlay は利用できる。
  - 検証条件: 初回フローが `Start → 説明 → 前菜 → すくい → 完食 → 生成食品選択 → 提供` の順に進み、「目の前の紙皿を見つめてください」という案内・QR 検出待ちを挟まない。
  - 出典: 2026-08-24 利用者要求、移管元チュートリアル仕様 §6。

### Model Selection, Loading, and Menus

- **FR12: 生成済み食品を再選択可能な履歴として扱うこと** (`MUST`)
  - 生成済み食品ごとに、少なくとも安定した item ID、表示名、preview image reference、3D model data reference、生成/利用可能状態を履歴項目として保持する。
  - Network mode の item ID は YummyService v2 の order identity に対応できる opaque value とし、QR GUID または mutable filename を server identity として扱わない。
  - 履歴は来場者セッション reset では失わず、利用可能な項目だけをメニューへ提示する。
  - 破損・未完了・取得失敗の項目を選択可能な正常項目として表示しない。
  - 検証条件: 一度生成済みとして登録された食品が次のメニュー表示でも識別可能で、同じ item を再度選択できる。
  - 出典: 2026-08-24 利用者要求「仮想メニュー（履歴一覧）」。

- **FR13: VR 空間内の仮想メニューから生成食品を選択できること** (`MUST`)
  - FreePlay で生成履歴を一覧表示し、来場者が controller で項目を選択できる。
  - 選択結果を食品 identity source とし、選択された model data を指定済み anchor へ提供する。
  - QR payload/GUID、QR 検出の成否、QR の pose を食品 item の選択に使わない。
  - 選択、loading、ready、error、retry の状態を利用者へ表示する。
  - Network mode の履歴・状態・artifact reference は YummyService v2 consumer API から取得し、Standalone local item は同じ application domain へ adapter で変換する。
  - 検証条件: QR を新たに読み込まず、履歴の任意項目を選んで対応する食品の提供を開始できる。
  - 出典: 2026-08-24 利用者要求。

- **FR14: メニューのプレビューは画像を先行読込し、3D モデルを一覧表示のために先行読込しないこと** (`MUST`)
  - 履歴一覧の表示に必要な preview image と metadata は先に取得/キャッシュできる。
  - メニューを開くためだけに、全項目の 3D payload の download、parse、instantiate を行わない。
  - 3D model data は選択時に既存 cache を再利用するか、必要な項目だけを取得・load する。
  - 画像取得失敗時は placeholder と再試行経路を用意し、他の履歴項目の表示を妨げない。
  - 検証条件: 複数項目のメニューを開いた時点で全項目の 3D model load が開始されず、各項目の画像または placeholder が表示される。
  - 出典: 2026-08-24 利用者要求「負荷軽減のため、プレビュー用には画像のみを先行して読み込む」。

- **FR15: iPad 等で生成済み食品を閲覧できる物理版メニューを提供すること** (`MUST`)
  - VR ヘッドセットを装着していない外部閲覧者が、生成履歴の食品を iPad 等の端末から一覧・詳細表示できる。
  - VR 内メニューと物理版メニューは同じ item identity と生成状態を解釈し、別物の履歴を作らない。
  - 未完成または閲覧不能な item は、その状態を viewer 上で識別できる。
  - 検証条件: VR で利用可能な生成済み item が iPad 等の viewer でも同じ ID/名称/preview として確認できる。
  - 出典: 2026-08-24 利用者要求「物理版メニュー（ビューアー）」。

### QR and Placement

- **FR16: QR コードを出現場所（アンカー）の指定だけに使用すること** (`MUST`)
  - QR の検出/認識結果は、モデルの出現 anchor の指定またはその designation flow の開始にのみ利用する。
  - QR をモデルの生成要求、食品 identity/GUID の選択、履歴 item の決定、model payload の download key に使用しない。
  - この要件は旧 intent の `FR5`/`NFR5`/`ADR-001` にある QR GUID 選択の継続方針を supersede する。
  - 検証条件: 同じ anchor designation のままメニューから異なる食品を切り替えられ、QR payload を変えても食品 item が自動的に切り替わらない。
  - 出典: 2026-08-24 利用者要求。

- **FR17: 選択食品を指定済みアンカーへ安定して表示すること** (`MUST`)
  - 食品を表示するには、有効な food item と使用可能な anchor/placement の両方が必要である。
  - designation 完了後の一時的な QR ロストや追跡揺れで、表示中モデルを別の食品へ切り替えたり、不用意に world origin へ移動したりしない。
  - 永続 Spatial Anchor と anchor-relative pose の既存要件を利用する場合、来場者 session reset ではその展示設定を消去しない。
  - 復元または designation に失敗した場合は未検証の pose へ食品を表示せず、再指定/再設定を案内する。
  - 検証条件: anchor 使用可能前は食品が誤表示されず、使用可能後はメニュー選択した食品が同じ配置基準に表示される。
  - 出典: 2026-08-24 利用者要求、`260821-spatial-anchor-food-placement` の非競合要件。

### Eating Interaction and Visual Feedback

- **FR18: 生成モデルへ簡易 AABB 当たり判定を付与すること** (`MUST`)
  - model load 後、生成モデルの形状から最も離れた2点を基準にした透明な箱型の Axis-Aligned Bounding Box を生成する。
  - collider は表示モデルと同じ placement/scale 変化へ追従し、render されない。
  - bounds を取得できないモデルは interaction ready とせず、診断可能な error/fallback 状態にする。
  - 検証条件: 形状の異なる生成モデルで透明 box collider が生成され、モデル表示位置・縮小に追従する。
  - 出典: 2026-08-24 利用者要求。二点の選定空間と厳密な AABB 算出手順は `Q3`。

- **FR19: スプーンですくった際のリアクションとイベントを発生させること** (`MUST`)
  - spoon interaction volume と食品 AABB の有効な接触/すくい成立を一回の食事 action として判定する。
  - 成立時に視覚または音による即時 feedback を出し、`FoodScooped` 相当の game event を発行する。
  - 接触中の毎 frame 多重発火を防ぎ、一つのすくい操作を設定された一回として数える。
  - 検証条件: 有効な scoop で一回の event/feedback が発生し、単なる非接触動作では発生しない。
  - 出典: 2026-08-24 利用者要求、移管元チュートリアルの `OnFoodScooped` 契約。

- **FR20: すくい成立時にコントローラー haptic feedback を提供できること** (`SHOULD`)
  - 対応 device では、すくい成立を短い振動で知らせられる。
  - haptic 非対応、controller 不明、実行失敗でも食事 action 本体を失敗させない。
  - 検証条件: 対応する Quest controller では成立時に一度振動し、haptic を無効化しても scoop/完食 flow が継続する。
  - 出典: 2026-08-24 利用者要求「コントローラーが震えてくれると嬉しいかも」。

- **FR21: 食べるたびに食品モデルを段階的に小さくすること** (`MUST`)
  - 有効な食事 action ごとに残量を減らし、食品 model と collider を徐々に縮小する。
  - 縮小はカジュアルで理解しやすい演出とし、複雑な断面 mesh を生成しない。
  - 残量は0未満にならず、一つの action で意図しない多段階減少を起こさない。
  - 完食までの食事 action の回数は要件で規定しない。実装既定値は5回とし、コード定数一箇所で変更できるようにする。
  - 検証条件: 複数回の食事 action に対してサイズが単調に小さくなり、collider と見た目が乖離しない。
  - 出典: 2026-08-24 利用者要求。

- **FR22: 食べカスを散らし、最後に食品を消滅させること** (`MUST`)
  - 各食事 action または設定した節目で、食品位置周辺に短時間の crumb effect を発生させる。
  - 残量が完食閾値へ達したら最後の feedback 後に食品 model、collider、関連する一時 effect を無効化または破棄する。
  - 完食時に `DishCleared` 相当の game event を一度だけ発行する。
  - 検証条件: 最終 action で食べカス演出後に食品が消え、同じ食品から完食 event が重複しない。
  - 出典: 2026-08-24 利用者要求、移管元チュートリアルの `OnDishCleared` 契約。

- **FR23: 食事イベントを Tutorial と FreePlay の双方で利用すること** (`MUST`)
  - `FoodScooped` と `DishCleared` は Tutorial 固有の fake state ではなく、ゲーム機能が発行する共通イベントとする。
  - Tutorial の TaskStep はイベントを購読して前菜の学習を進め、FreePlay は同じ完食 event から outro/次操作へ進む。
  - 本実装が利用可能になった後は、破棄ボタンを完食として扱う暫定 dummy 経路を本番 flow から外す。
  - 検証条件: 同じ検出実装が Tutorial/FreePlay の両方で動作し、TutorialRunner が detector の具象を参照しない。
  - 出典: 移管元チュートリアル仕様・利用ガイド、2026-08-24 利用者要求。

- **FR24: セッション状態と永続データの寿命を分離すること** (`MUST`)
  - 来場者ごとの選択、食品 instance、食事残量、crumb、menu visibility、loading/error の一時状態は session reset で初期化する。
  - 生成履歴、preview、model cache の保持方針、展示運営者が確定した anchor/placement は明示的な削除・期限切れ・容量 policy がない限り session reset で消去しない。
  - 外部 viewer は削除済み/無効化済み item を新規に表示可能な食品として扱わない。
  - 検証条件: 2人目の来場者は前セッションの食べかけ食品を引き継がず、生成履歴と有効な展示配置は再利用できる。
  - 出典: 移管元チュートリアル仕様 §5、移管元利用ガイド §4、2026-08-24 利用者要求。

### YummyService v2 API Integration

- **FR25: YummyService v2 契約だけを新しい Network mode の API 境界として使用すること** (`MUST`)
  - Target contract は YummyService `ru322/main@97c9ed75980ec398fe75159bd4e011b489112433` の `contracts/v2/openapi.yaml`（`2.0.0-draft`）と `contracts/v2/README.md` とする。採用 snapshot の OpenAPI は104 paths、124 schemasである。
  - **YummyVerseUnity における v1 API は廃止済みであり、今後一切使用しない。** Production、development、test、demo、障害時 fallback、migration compatibility、Standalone の代替を含む全 runtime から `/v1/...` への outbound request を禁止する。
  - 旧 Yummy Control Server の `/{guid}/model` と QR GUID download trigger も v2 integration の fallback として使用しない。
  - v1 client/DTO/configuration/mock を新しい runtime dependency として追加・維持しない。例外は「v1 response/URL を確実に拒否する」negative compatibility fixture だけで、v1 server へ実 request を送らない。
  - Unity の正式な v2 operation は history `GET /v2/devices/unity/orders`、status `GET /v2/devices/unity/orders/{order_id}`、selected artifact download、Hardware Payload、Payload ACK とする。`/v2/menu` は公開 sample menu であり、生成 order の代用にしない。
  - Client は接続先の v2 compatibility/version を検証でき、非互換 server を成功扱いにしない。現行 contract には deployed server の runtime negotiation がないため、これは未達 gap として扱う。
  - `servers.url` は placeholder のため、実 deployment host、TLS、Device token が確定するまで本番接続を成功扱いにしない。
  - 検証条件: Source/config/build/request trace に runtime v1 route、旧 GUID model route、Admin static token がなく、正式な Unity route/auth と snapshot が確認できない接続先で明示 error になる。全 test 環境でも v1 server への outbound request は0件である。
  - 出典: 2026-08-30 YummyService `ru322/main` OpenAPI/README、`ADR-006`。

- **FR26: v2 の order/stage workflow state を欠落なく解釈すること** (`MUST`)
  - OrderState `DRAFT`, `QUEUED`, `PROCESSING`, `AWAITING_ADMIN_REVIEW`, `COMPLETED`, `REJECTED`, `FAILED`, `CANCELED` を区別する。
  - Domain mapping は StageState `PENDING`, `QUEUED`, `PROCESSING`, `COMPLETED`, `COMPLETED_WITH_WARNING`, `FAILED`, `CANCELED` と5 stageを order state と別に保持する。ただし現行 Unity Device の `CustomerOrderStatus` projection は `analysis`、GLB、WAV の状態だけを返し、全5 stage、moderation、retrieval、I23D の個別状態は返さない。欠落した state を client が推測してはならず、全 stage 表示は contract gap とする。
  - `AWAITING_ADMIN_REVIEW` を全 branch 停止と表示せず、`IMAGE_TO_3D` が独立して進行し得ることを表現する。
  - `COMPLETED_WITH_WARNING` は許可された Example Retrieval/Zero Shot outcome として扱い、任意 failure を warning success に変換しない。
  - 未知 enum 値を `COMPLETED` または menu-ready に推測変換しない。
  - 検証条件: domain fixture では全既知 state/未知 stateを確認し、Device fixture では未提供の stage/detail を捏造せず、menu-ready 判定を `COMPLETED` と downloadable artifact に限定する。
  - 出典: YummyService v2 workflow contract、`CustomerOrderStatus` device projection。

- **FR27: 生成履歴を取得する v2 API capability を要求すること** (`MUST`)
  - Quest/Physical Viewer は customer-visible な generated order/item を列挙できる必要がある。
  - `GET /v2/devices/unity/orders` は `DeviceOrderListResponse` を返し、stable `order_id`、`food_name`、OrderState、analysis/GLB/WAV readiness、cursor pagination、安定した sort/filter semantics を持つ。query は `state`、`q`、`food_name`、`limit`（1〜100、default 50）、opaque signed `cursor` である。
  - 現行 response に preview image reference/metadata はなく、history から preview を提供する要件は未達である。
  - Terminal failure/rejection/cancel と processing/review を Ready item から区別できる。
  - `has_more=false` では `next_cursor=null` とし、cursor を decode/編集/再生成したり filter の異なる request へ流用したりしない。
  - 検証条件: 複数 page、同時追加、重複 page、空履歴、各 terminal/non-terminal state で重複のない履歴を構成でき、preview がないことを GLB/WAV sample で補完しない。
  - 出典: `FR12`〜`FR15`、`API-CAP-01`、`DeviceOrderListResponse`。

- **FR28: 一つの order と全 stage の状態・選択 revision を取得できること** (`MUST`)
  - API は `GET /v2/devices/unity/orders/{order_id}` で `CustomerOrderStatus` を返す。OrderState、`analysis`、GLB、WAV の customer-safe projection は利用できるが、全5 stage の StageState、warning/review/failure detail、selected revision pointer は現行 Device response に含まれない。
  - Client は order-level review と Food Analysis approval/status を混同しない。
  - response には `updated_at` があるが、order status の ETag、event、明示的な monotonic version は定義されていない。古い response の巻き戻し防止と全 stage/detail の公開は未解決である。
  - 検証条件: 利用可能な response では terminal/downloadable を巻き戻さず、未提供の review/detail/I23D state を推測しない。全 stage fixture は domain/admin contract 側で確認する。
  - 出典: YummyService v2 workflow contract、`CustomerOrderStatus`、`API-CAP-02`。

- **FR29: 選択された immutable artifact revision の metadata を取得できること** (`MUST`)
  - Preview 候補の `SOURCE_IMAGE_NORMALIZED`、model の `GLB`、将来利用する場合の `WAV` について、`artifact_id`, `artifact_type`, `revision`, `sha256`, `verified` を取得する。
  - selected/current pointer と immutable revision を分離し、new revision が存在するだけで自動的に current とみなさない。
  - Domain/Admin の `ArtifactRevision` は上記 metadata を持つ。Unity Device の `CustomerOutputStatus` は downloadable なときの `artifact_id` だけを返し、`artifact_type`、`revision`、`sha256`、`verified`、`size_bytes` は返さない。selected verified GLB/WAV の download gate は server 側で適用される。
  - GLB は `COMPLETED` order の `downloadable=true` かつ `artifact_id` がある場合だけ model load candidate にする。metadata 不足を guessed URL で補完しない。
  - customer-visible artifact の範囲は API auth/visibility contract に従い、admin-only artifact を client から取得しない。
  - 検証条件: wrong type、unverified、missing selection、older/newer unselected revision を server/client boundary で拒否できる。Unity に checksum metadata が公開されるまで FR31 の client-side byte gate は未達とする。
  - 出典: YummyService v2 `ArtifactRevision`、`CustomerOutputStatus`、`API-CAP-03`。

- **FR30: Preview image と選択 GLB を別 operation/lifecycle で download できること** (`MUST`)
  - 現行 Unity Device API に `SOURCE_IMAGE_NORMALIZED` の preview operation はない。`/v2/menu/{menu_item_id}/glb` と `/wav` は published sample であり、order preview の代用にしない。
  - Food selection 後は `GET /v2/devices/unity/orders/{order_id}/artifacts/{artifact_id}/download` で selected GLB/WAV を取得する。成功 content type は `model/gltf-binary` または `audio/wav`、`Content-Disposition` に suggested filename がある。
  - Download contract の size limit、redirect、range/resume、cache validation、retry policy は未定義であり、preview failure の UI policy とともに contract gap とする。
  - 検証条件: menu open 時の request trace に未選択 GLB/WAV がなく、selection 後は status が返した selected artifact だけを取得する。preview が必要な acceptance は preview operation 公開まで未達とする。
  - 出典: `FR14`、YummyService v2 `ArtifactType`、`API-CAP-04`/`API-CAP-05`、Unity Device routes。

- **FR31: Artifact の identity と SHA-256 integrity を検証してから利用すること** (`MUST`)
  - Download bytes の SHA-256 を metadata と比較し、一致前に preview decode、glTF load、共有 cache publish を完了扱いにしない。
  - Cache key は少なくとも artifact ID、revision、SHA-256 を含み、固定 filename や order ID だけで immutable revision を上書きしない。
  - Mismatch、途中 download、破損 cache は削除/隔離して再取得可能にし、verified flag だけを client-side byte verification の代替にしない。
  - `ArtifactRevision.sha256` は domain/Admin schema に存在するが、Unity Device status/download response に GLB/WAV digest、revision、checksum header は定義されていない。`HardwarePayload.payload_sha256` は payload digest であり、GLB/WAV の代用にしない。
  - 検証条件: byte 改変、truncated response、同一 artifact の revision change、並行 download で誤った model を load しない。Unity 側の SHA-256 gate は server metadata/header が追加されるまで `NOT-READY` とする。
  - 出典: YummyService v2 immutable artifact/SHA-256 contract、Unity Device artifact response、`ADR-007`。

- **FR32: Quest と Physical Viewer が同じ認可された v2 catalog を参照すること** (`MUST`)
  - Quest と iPad 等の viewer は同じ order/item/artifact identity と state semantics を使用する。
  - Unity Device には `deviceBearerAuth` と `device_type=UNITY` の token issue/rotate/revoke contract が定義されている。Client へ admin/worker credential、Mock static token を埋め込まず、token plaintext をログへ出さない。
  - Customer/Viewer の scope、token lifetime/refresh、同一 catalog visibility は別途確定する。Unity Device route の all-order scope を customer visibility と同一視しない。
  - Viewer が unauthorized/offline の場合は private artifact を表示せず、VR session の進行とは独立した error を示す。
  - 検証条件: Unity token の401/403、revoke、role boundary を確認し、Quest と viewer の同一 item identity/artifact revision 共有は viewer contract 公開後に確認する。
  - 出典: `FR15`、YummyService v2 `deviceBearerAuth`/device lifecycle、`API-CAP-06`。

- **FR33: v2 ProblemDetails と operation-specific error/retry contract を扱うこと** (`MUST`)
  - `application/problem+json` の `type`, `title`, `status` と任意の `detail`, `instance`, extension fields を解析できる。
  - 現行 operation の主要 status は、history `400/401/403`、status/download `401/403/404`、payload `202/304/503` と `401/403/404`、ACK `400/401/403/404/409` である。rate limit、timeout/cancellation を含む retryability の全 operation mapping は未確定である。
  - Client は401/403、not found、conflict、rate limit、validation、server/unavailable、timeout/cancellation を同一の generic failure に潰さない。
  - Retryable operation だけを cancellation-aware backoff で再試行し、session reset 後の response を次 session へ反映しない。
  - ProblemDetails の未知 extension field を理由に response 全体を拒否しない。
  - 検証条件: operation ごとの problem fixture、malformed response、timeout、cancel、late response で UI/state/cache が汚染されない。
  - 出典: YummyService v2 `ProblemDetails`、Unity Device route status、必要 capability `API-CAP-07`〜`API-CAP-09`。

### Unified Food Selection UI and Standalone Continuity

- **FR34: チュートリアル完了後に API/Standalone 共通の食品選択 UI を表示すること** (`MUST`)
  - 前菜による Tutorial が S14 で完了した後、同じ session/scene の FreePlay で食品一覧 UI を表示する。
  - 一つの食品選択 UI に、YummyService v2 API から取得した選択可能な生成食品と、Standalone catalog に保存されているローカル食品の両方を同時に表示する。
  - 各 item は少なくとも表示名、preview/placeholder、利用可能状態、`Network (YummyService v2)` または `Standalone (Local)` の source を識別できる。
  - Network と Standalone の ID namespace を分離し、同じ名前/GUID/内部値でも別 item を誤って上書きしない。
  - Network item の選択は v2 selected verified GLB flow、Standalone item の選択は local loader flow へ dispatch し、どちらも同じ anchor placement と eating interaction へ合流する。
  - API が未接続・offline・error の場合も Standalone item を UI から消さず、Network 部分の状態だけを error/offline として示す。
  - 検証条件: Tutorial 完了後の一つの UI に Network/Standalone item が同時表示され、それぞれを選択すると対応する loader だけが動き、同じ food presentation flow へ到達する。
  - 出典: 2026-08-24 利用者確認、既存 Canonical Experience Flow S15〜S17。

- **FR35: Standalone Mode を API 非依存の恒久機能として維持すること** (`MUST`)
  - Standalone Mode は端末に保存された local 3D model/catalog を表示する機能として今後もサポートする。
  - Standalone item の一覧、選択、model load、表示に YummyService v2 または廃止済み v1 API への request を必要としない。
  - API endpoint 未設定、network unavailable、authentication failure、v2 contract mismatch の場合でも、有効な Standalone item は選択・表示できる。
  - Local file の欠落・破損・未対応形式は item 単位で unavailable/error とし、他の Standalone item と UI 全体を停止させない。
  - Standalone item も anchor designation/placement、AABB、scoop、縮小、crumb、DishCleared、session reset の共通要件に従う。
  - Session reset は表示中の local food instance を消去するが、端末に保存された Standalone model/catalog 自体は削除しない。
  - 検証条件: Network を無効化した状態でアプリを開始し、Tutorial 完了後の食品選択 UI から Standalone item を選び、指定 anchor への表示と食事 flow を完了できる。
  - 出典: 2026-08-24 利用者確認、既存 Standalone implementation boundary。

## Non-Functional Requirements

- **NFR1: Unity の既存 architecture と非同期方式を維持すること**
  - Unity 6、C#、UniTask または Unity Awaitable、R3、Extenject、Model/ViewModel/View の責務分離を現行基準とする。
  - 非同期 API は `CancellationToken` を受け取り、例外を黙って成功扱いにしない。
  - 検証方法: assembly/reference review、DI resolution、キャンセル/例外経路の EditMode/PlayMode test。

- **NFR2: チュートリアルから FreePlay へシームレスに移行すること**
  - Tutorial 完了から FreePlay 開始までに scene transition、ゲーム再起動、不要な暗転、同じ asset の再初期化を発生させない。
  - 検証方法: 通し操作と frame/log capture で状態遷移を確認する。

- **NFR3: 中断後3秒以内に Attract へ復帰すること**
  - 任意 step、model loading、menu 操作、食事途中からの中断を対象とする。
  - 検証方法: trigger 入力から Attract/UI/food reset 完了までを計測する。

- **NFR4: 連続10セッションで状態汚染や実行資源の増加を起こさないこと**
  - 無操作、途中離脱、正常完走を混在させる。
  - 検証方法: event subscriber、CancellationTokenSource、food/collider/effect instance、memory の推移を記録する。

- **NFR5: メニュー一覧のための 3D 先行ロード負荷を発生させないこと**
  - メニュー表示時の network/CPU/GPU/memory 使用は image/metadata に限定し、3D payload は選択 item だけを対象にする。
  - 「即座」の数値 latency target と cache hit 条件は `Q1` で確定するまで合格を主張しない。
  - 検証方法: network request、model parse/instantiate、memory allocation の instrumentation。

- **NFR6: Quest と外部 viewer の device boundary を明示すること**
  - VR interaction/anchor/haptic は Meta Quest 3 実機で検証し、Editor/PCVR の代替結果を実機合格と扱わない。
  - iPad 等の viewer は対象 browser/app、network、表示形式を決定した後、その実機で検証する。
  - 検証方法: device ごとの結果と未実施項目を分離した test report。

- **NFR7: 失敗しても来場者を立ち往生させないこと**
  - anchor、preview、model load、scoop bounds、viewer sync の失敗は error/placeholder/retry/rescue/Attract 復帰のいずれかへ到達する。
  - 検証方法: 各統合境界の timeout、欠落、無効 data、通信断を fault injection する。

- **NFR8: 現場診断に必要な観測性を持つこと**
  - session ID、AppState、step ID、item ID、anchor state、preview/model load state、scoop、remaining portion、rescue/reset を相関できるログを持つ。
  - 検証方法: 一つのセッションをログだけから時系列再構成できることを review する。

- **NFR9: 食事表現を簡易かつ低負荷に保つこと**
  - 当たり判定は透明 box collider、変形は scale/段階表現、食べカスは再利用可能な軽量 effect を基本とする。
  - 複雑な断面 mesh、連続破壊 simulation、毎 frame の bounds 再計算を必須にしない。
  - 検証方法: Quest 実機の profiler と、複数回の scoop 後の collider/visual 整合性確認。

- **NFR10: v2 draft contract の revision を固定・監査すること**
  - Build/release ごとに採用する YummyService repository commit と OpenAPI version/checksum を記録する。
  - `2.0.0-draft` の更新を自動採用せず、schema/path/state/security diff と client impact review を行う。
  - 検証方法: CI/verification report に contract revision を出力し、fixture/generated client が同じ revision に由来することを確認する。

- **NFR11: v2 非互換・未知状態・未検証 artifact では fail closed すること**
  - Version/state/schema/artifact type/integrity を推測で成功へ変換しない。
  - Compatibility error は diagnostic に server URL、contract revision、operation、state/type を含めるが token/secret を含めない。
  - 検証方法: v1 response、未知 enum、missing fields、wrong artifact type、SHA mismatch の contract test。

- **NFR12: API request/download を cancellation-aware かつ session-safe にすること**
  - History、status、preview、GLB の request は timeout と CancellationToken を持ち、retry/backoff も同 token で停止する。
  - 遅延 response は session/item generation token または request identity で stale と判定し、new session/selection を上書きしない。
  - 検証方法: network delay、out-of-order response、cancel during download、rapid item reselection の PlayMode/integration test。

- **NFR13: Client credential と artifact transport を production-safe にすること**
  - Production endpoint は TLS と v2 で確定した customer/device authentication を使用し、admin/worker/static development token を build に含めない。
  - Token、authorization header、signed download URL、個人情報を通常 log に出さない。
  - 検証方法: build secret scan、proxy/log review、expired/revoked/wrong-scope token test。v2 auth/TLS contract 公開前は production-ready と判定しない。

- **NFR14: Large artifact を余分な全量コピーなしに保存・検証すること**
  - GLB download は可能な限り streaming/file download と incremental SHA-256 を用い、bytes→base64→bytes の変換を行わない。
  - Temp/cache filename は artifact revision ごとに一意かつ atomic publish 可能にし、並行 request で共有固定名を使わない。
  - 検証方法: representative large GLB の peak memory、temporary files、concurrent selection、cache hit/miss を profiler/file inspection で確認する。

## Canonical Experience Flow

| Phase | Canonical ID | Type/owner | Content | Completion |
|---|---|---|---|---|
| Attract | S1 | AppState/Config | スタートボタンを案内 | Start input |
| Tutorial | S2 | Narration | YummyVerse と AI 生成食感体験を紹介 | Button or time |
| Tutorial | S5 | Narration | AI シェフ/前菜の準備を案内 | Time |
| Tutorial | S6 | Choice | 初回/繰り返し利用を判定 | Choice or timeout |
| Tutorial | S6' | Narration | リンゴ等の前菜を食べるよう案内 | Button |
| Tutorial | S7 | Game command | 前菜を指定 anchor へ提供 | Food ready |
| Tutorial | S8 | Task | スプーンですくう。催促/デモを内包 | FoodScooped |
| Tutorial | S11 | Task | そのまま完食する。催促を内包 | DishCleared |
| Tutorial | S14 | Narration | 食事操作の理解を確認 | Time |
| FreePlay | S15 | Narration | 食べたい生成食品の選択を促す | Button |
| FreePlay | S16 | Choice/Menu | 一つの食品選択 UI に YummyService v2 由来の生成食品と Standalone local 食品を統合表示 | Menu selection |
| FreePlay | S17 | Game command | 選択された verified GLB/Standalone 食品を指定 anchor へ提供 | Food ready |
| FreePlay | S18 | Narration/Event | 完食への謝辞 | DishCleared |
| Outro | S19 | Narration | 再来を案内 | Time, then Attract |

- S15 以降は TutorialSequence に含めず、FreePlay/Outro が担当する。
- S7/S17 は Narration の副作用として Game の具象を直接呼ばず、command boundary か各 state owner の責務として実行する。
- S3 の「目の前の紙皿を見つめてください」という案内と QR 検出待ちは、2026-08-24 の利用者指示により現行 TutorialSequence から削除する。Anchor designation 自体の製品責務は `FR16`/`FR17` に残すが、チュートリアル進行条件にはしない。

## Acceptance Scenarios

- **AC1 Normal first visit**: Attract で Start → リンゴ等の前菜 → scoop feedback → 段階縮小/crumb → 消滅 → 仮想メニュー → 生成食品選択 → 利用可能な指定済み anchor へ提供、が同一 scene/session で完了し、紙皿注視の案内・待機を挟まない。
- **AC2 Menu load isolation**: 20件以上を含む代表的な履歴一覧を開いても、未選択19件の 3D payload は load/instantiate されず、画像または placeholder が表示される。件数は負荷観測用で、製品の保持上限を決定するものではない。
- **AC3 QR responsibility**: QR designation 後に別の履歴 item を選択しても anchor は再利用できる。QR payload の違いだけでは食品 identity が変わらない。
- **AC4 Eating interaction**: 生成モデルへ透明 AABB が付き、有効な scoop ごとに event は一度、サイズは単調減少、crumb が発生し、最終 action で model/collider が消え DishCleared が一度だけ発生する。
- **AC5 Rescue**: S8/S11 で操作しないと hint delay 後にヒント、rescue timeout 後に設定 policy が実行され、無限待機しない。
- **AC6 Abort and reuse**: 任意 step/food portion で staff reset または UserAbsent を発生させると3秒以内に Attract へ戻る。次 session に食べかけや UI は残らず、生成履歴と有効な anchor 設定は残る。
- **AC7 Repeated sessions**: 無操作/途中離脱/正常完走を混ぜた10セッションで subscriber、食品 instance、collider、crumb effect、loading task のリークや状態汚染がない。
- **AC8 Physical viewer**: VR メニューで利用可能な代表 item が iPad 等で同じ item ID/名称/preview/状態として閲覧できる。3D 表示の必須性は `Q2` 解決後に追加判定する。
- **AC9 Documentation independence**: `docs/` が存在しない前提で、本ファイル、domain design、source migration map、space-level knowledge から全 FR/NFR、体験フロー、旧要件の supersession、未解決事項を追跡できる。
- **AC10 v2-only adapter**: Network mode の request trace/source review に `/v1/`、`/{guid}/model`、QR GUID download trigger がなく、v2 compatibility を確認できない server では明示 error になる。
- **AC11 Workflow mapping**: 全 OrderState/StageState fixture を入力し、review/warning/failure/cancel と I23D 独立進行を誤って Ready/全停止として表示しない。
- **AC12 Artifact integrity**: `COMPLETED` order の selected verified GLB を download し、SHA-256 一致後だけ glTF load/cache publish する。改変・truncated・wrong type・unselected revision は拒否する。
- **AC13 API-driven menu**: `GET /v2/devices/unity/orders` の複数 page を opaque cursor で重複なく一覧化し、選択前の order GLB/WAV download は0件である。現行 API に preview operation がないため、public sample を preview として補完しない。
- **AC14 Cross-device identity**: Unity Device route では認可された token で同一 order と `artifact_id` を参照し、wrong-role/device token は拒否される。iPad viewer との同一 selected revision、artifact checksum、visibility isolation は viewer contract 公開後の追加検証とする。
- **AC15 Contract gate**: `ru322/main@97c9ed7...` には Unity Device の history/status/artifact/payload/ACK paths と schemas があるため、v2 route/schema を `READY` とする。ただし preview、全 stage/detail、Unity artifact checksum、production host/TLS/secret delivery、runtime compatibility negotiation が未解決のため、full consumer/production HTTP integration は `PARTIAL`/`NOT-READY` と判定する。
- **AC16 Unified post-tutorial menu**: S14 完了後、scene transition なしで一つの食品選択 UI が開き、Network item と Standalone item が source を識別可能な状態で同時表示される。各 source から一件ずつ選択し、正しい loader/food data が同じ anchor/eating flow へ渡る。
- **AC17 Offline Standalone continuity**: API endpoint を未設定または network unavailable にしても、食品選択 UI は Standalone item を表示し、local model の選択・表示・完食を完了できる。Network error が local flow を block しない。

## Constraints and Prohibited Patterns

- Unity version/package version は repository manifest/version file を根拠にする。
- TutorialStep の具象を Narration/Task/Choice より増やさず、step ごとの enum と巨大 switch を作らない。
- TutorialRunner/Step から Game/View の具象を直接参照しない。
- step 間の任意 jump、チュートリアル専用 scene、timeout のない TaskStep を作らない。
- ScriptableObject の Condition/Step に session runtime state を保持しない。
- session reset で永続 anchor/placement や生成履歴を暗黙に削除しない。
- QR を food identity、model generation、history selection の入力へ戻さない。
- メニュー一覧のために全 3D model を先行 download/parse/instantiate しない。
- v1 API は廃止済みであり、production/development/test/demo/fallback/migration/Standalone を問わず runtime から一切呼ばない。v1 rejection 用の local negative fixture だけを許容する。
- v2 OpenAPI にない path/method/header/response/auth を推測して production contract として固定しない。
- `verified=false`、SHA-256 mismatch、wrong ArtifactType、unselected revision を表示/load しない。
- Food Analysis `confidence` から gameplay、haptic、muscle/electrical control threshold を自動導出しない。
- GLB bytes を base64 へ往復変換したり、全 artifact で同じ temp filename を使ったりしない。
- Client build に admin/worker credential や development static token を埋め込まない。
- API error/offline を理由に Standalone item を一覧から消したり、Standalone Mode 全体を利用不能にしたりしない。
- Standalone local model/catalog の利用を v1 API fallback とみなさない。Standalone は API request を行わない独立 source である。
- 食事表現に複雑な断面生成を必須化しない。
- `docs/` を現行要件の必須参照先にしない。

## Out of Scope

- 複雑な断面生成、物理的に正確な切断・流体・咀嚼 simulation。
- 複数ユーザーが同一食品を同期して食べる multi-user interaction。
- 生成 AI/model pipeline 自体の変更。ただし生成結果を履歴へ登録する contract は対象。
- Viewer の配布・認証・network architecture の最終選定。
- 数値化されていない性能目標をこの文書だけで確定すること。
- 現在の Unity app から order intake、source image upload、submit、Admin review、worker lease/retry を実行すること。将来必要になった場合は YummyService v2 の `OrderInput`/moderation/auth contract を別途要件化する。
- v2 contract に transport operation がない状態で v1 endpoint を呼んで暫定互換とみなすこと。

## Sources

- **SRC-1 — 移管元チュートリアル実装指示**: データ駆動 step、4 AppState、event/presenter separation、3 step types、5 condition types、runner logging、cancellation/reset、hint/rescue、S1〜S19、acceptance、anti-pattern を規定していた。移管元パスは `docs/tutorial-requirement.md` だが、本ファイルに必要内容を再記載済み。
- **SRC-2 — 移管元チュートリアル利用ガイド**: 現行 event/command bus、DI scope、asset editing、reset responsibilities、debug/test flow、既存実装との差分、anti-pattern を説明していた。移管元パスは `docs/tutorial-usage.md` だが、本 intent の requirements/domain design と shared knowledge に必要内容を再記載済み。
- **SRC-3 — 2026-08-24 利用者追加要求**: VR 内 start、基本食品→注文食品、QR を anchor 指定のみに変更、生成履歴メニュー、画像 preview、iPad 等の物理 menu、AABB、scoop reaction/任意 haptic、段階縮小、crumb、最終消滅。
- **SRC-4 — 2026-08-24 文書統合要求**: 将来 `docs` を削除しても `aidlc` 単体で要件が分かること。
- **SRC-5 — YummyService v2 contract**: `https://github.com/YummyVerseVR/YummyService` の `ru322/main@97c9ed75980ec398fe75159bd4e011b489112433`、`contracts/v2/openapi.yaml` (`2.0.0-draft`) と `contracts/v2/README.md`。OpenAPI は104 paths/124 schemasで、Unity Device の history/status/artifact/payload/ACK、deviceBearerAuth、PublicMenu、ProblemDetails が定義されている。一方、deployment URL、Unity preview、全 stage customer projection、artifact checksum exposure、runtime compatibility negotiation は未解決である。
- **SRC-6 — Current Unity integration evidence**: `NetworkFoodCatalogSource.cs`、`NetworkFoodLoader.cs`、`NetworkConnectionTester.cs` は開発用 `/v2/admin/menu` と固定 `admin-demo-token`、menu URL を使っており、正式な `/v2/devices/unity` order/artifact contract への migration が必要である。`YummyServiceV2Contract.cs` の snapshot constant も更新対象である。
- **SRC-7 — 2026-08-24 Standalone/UI clarification**: Standalone Mode は今後も使用する。Tutorial 完了後に食品一覧 UI が存在し、その UI は YummyService v2 API 由来の食品と端末保存済み Standalone 食品を同時に表示する。
- 詳細なセクション単位の移管対応は `source-migration-map.md` を参照する。そこにも source requirement の要約を持たせ、`docs` の存在を前提にしない。
- v2 API の自己完結した snapshot と required capability は `knowledge/aidlc-shared/yummy-service-v2-api.md`、consumer mapping は `inception/contract-design/contract-summary.md` を参照する。

## Assumptions & Open Questions

- **Q1 (Blocking for model-delivery Unit)**: 「即座に呼び出せる」の selection-to-visible 目標時間、cache hit/miss 別の閾値、offline fallback、cache 容量/eviction policy はいくつか。
- **Q2 (Blocking for physical-viewer Unit)**: 物理版メニューは preview image/metadata 閲覧だけでよいか、3D viewer を含むか。対象端末、配布方式、認証、LAN/Internet、更新同期の要件は何か。
- **Q3 (Resolved 2026-08-28)**: 「最も離れている2点」は、食品ルートのローカル座標系へ変換した全メッシュ頂点集合の最小コーナーと最大コーナーとする。この2点は箱の対角線を張り、各軸 extent はその差で決まる。結果は当該座標系での通常の AABB と同義であり、形状によらず必ず1つ求まる。頂点を読み出せないメッシュは、そのメッシュ自身の bounds の8隅で代用する。
- **Q4 (Non-blocking)**: Haptic を展示版の必須受け入れに昇格するか。現時点は `SHOULD`。
- **Q5 (Blocking for anchor Unit)**: QR designation を既存の運営者設定済み Meta Spatial Anchor の選択に使うのか、QR pose から新規/一時 anchor を作るのか。既存 Cube/UUID/relative pose flow との優先関係は何か。
- **Q6 (Partially resolved 2026-08-30)**: Unity Device の history/status/artifact/payload/ACK path、method、schema、主要 status は `ru322/main` で確定した。全 consumer capability、preview、全 stage/detail、production host は未確定である。
- **Q7 (Partially resolved 2026-08-30)**: Unity は `deviceBearerAuth` と `device_type=UNITY` の issue/rotate/revoke を使う。Customer/Physical Viewer の auth、visibility scope、secret delivery は未確定である。
- **Q8 (Blocking for Preview Unit)**: Customer-visible preview が selected `SOURCE_IMAGE_NORMALIZED` か、selection/media type/download visibility をどう公開するか。現行 Unity Device API に preview operation はない。
- **Q9 (Partially resolved 2026-08-30)**: History の state/q/food_name/limit/cursor、sort semantics は確定した。order change detection、rate limit、cache validation、polling interval は未確定である。
- **Q10 (Partially resolved 2026-08-30)**: GLB/WAV device download の path、selected/completed/verified gate、media type、Content-Disposition、auth/status は確定した。checksum、max bytes、redirect、range/resume、timeout/retry guidance は未確定である。
- **Q11 (Resolved 2026-08-30)**: Device `downloadable=true` は selected verified output of a `COMPLETED` order に限定される。Unity は GLB が I23D で先に生成されても、order 全体が `COMPLETED` になる前に customer menu へ公開しない。
- 仮定: リンゴは例であり、同程度に認知しやすい固定の前菜へ差し替え可能である。
- 仮定: 生成履歴は来場者セッションより長く保持されるが、永続期間と削除 policy は別途決定する。

## Review

- Requirements capture status: `READY`
- v2 domain contract mapping: `READY`
- v2 Unity Device route/schema contract: `READY`
- v2 full consumer contract: `PARTIAL`
- v2 production HTTP contract: `NOT-READY`
- Construction readiness: `NOT-READY`
- Approval basis: 2026-08-24 の明示要求、移管元2文書、YummyService `ru322/main` normative contract snapshot。詳細は `yummy-service-v2-unity-api.md` と API contract review を参照する。
- Reviewed at: 2026-08-30
