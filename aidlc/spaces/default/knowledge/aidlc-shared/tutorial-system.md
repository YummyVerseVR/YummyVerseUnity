# Tutorial System Knowledge

## Authority and Scope

この文書は YummyVerse の既存チュートリアル実装境界と運用知識を `aidlc` 内へ保存する。現行の製品要件は `intents/260824-guided-food-experience/inception/requirements-analysis/requirements.md` を正とする。特に QR の食品 GUID 選択に関する旧挙動は現行要件ではなく、食品 identity は生成履歴メニュー、QR は anchor designation のみを担当する。

## Runtime Architecture

```text
Game/Input/QR ──publish──> GameEventBus ──subscribe──> TutorialCondition
                                                              ↑
Presenter interfaces <── TutorialStep (ScriptableObject) <── TutorialSequence
                              │                               ↑
                              └──request──> GameCommandBus   TutorialRunner
                                                │             ↑
                                                v       SessionController
                                         GameCommandRouter
```

- App state: `Assets/YummyVerse/Scripts/Model/AppStateMachine.cs`
- Session/flow: `Assets/YummyVerse/Scripts/ViewModel/Tutorial/SessionController.cs`、`TutorialRunner.cs`、`FreePlayFlow.cs`
- Step/condition assets: `Assets/YummyVerse/Scripts/ViewModel/Tutorial/SO/` と `Assets/YummyVerse/Data/Tutorial/`
- Event/command: `Assets/YummyVerse/Scripts/Model/GameEventBus.cs`、`GameCommandBus.cs`、`Assets/YummyVerse/Scripts/View/Tutorial/GameCommandRouter.cs`
- Presenters/views: `Assets/YummyVerse/Scripts/ViewModel/Tutorial/*Presenter.cs`、`Assets/YummyVerse/Scripts/View/Tutorial/*View.cs`
- Reset: `Assets/YummyVerse/Scripts/Model/GameResetter.cs`
- DI: `Assets/YummyVerse/Scripts/ViewModel/DI/TutorialInstaller.cs`

依存は一方向とし、TutorialRunner/Step は Game/View の具象を参照しない。Game は Tutorial の有無を知らず、成立した event を発行する。

## Current Data Assets

`Assets/YummyVerse/Data/Tutorial/` には次が存在する。

- Conditions: button、time、button-or-time、QR、FoodScooped、DishCleared。
- Steps: S2、S3、S5、S6、S6'、S8、S11、S14、S15、S16、S18、S19。
- Sequences: `TutorialSequence_Main`、`TutorialSequence_FreePlay`。
- Feedback: `SuccessFeedback_OK`。
- Localization: `TutorialStrings` と日本語 table。
- Configuration: `TutorialConfig.asset`。

既定 asset は `Assets/YummyVerse/Editor/TutorialAssetBuilder.cs` が生成する。生成処理は既存 asset/文言を上書きしない冪等動作を維持する。

## Editor and Scene Setup Baseline

移管元 guide が要求していた baseline。scene/prefab の serialized wiring は変更時に Unity Editor で再確認する。

1. Unity Localization Settings に日本語 locale を用意する。
2. Editor menu `YummyVerse > Tutorial > Create Default Tutorial Assets` で不足 asset を生成する。
3. `Assets/YummyVerse/Scripts/InputActions/Restaurant.inputactions` の generated wrapper を更新し、Start と StaffReset action を利用可能にする。
4. Restaurant scene の scene-level DI scope へ `TutorialInstaller` と共有 ViewModel binding を置く。Presenter View を child `GameObjectContext` に隔離しない。
5. Message、Hint、Feedback、Choice、Voice、Debug HUD、GameCommandRouter を同じ scene/session に配置する。Tutorial 専用 scene は作らない。
6. QR lost/designation lost を runtime event へ接続する。新要件への migration では旧 `QrPlateDetected`/food GUID semantics を AnchorDesignated semantics へ置き換える。
7. FoodScooped/DishCleared の本実装が未接続な環境だけ dummy event を用い、本番 flow では破棄操作を完食の代替にしない。

## Editing Rules

| Change | Primary asset/boundary |
|---|---|
| Text | `TutorialStrings` localization table |
| Narration wait | Time condition asset |
| Hint/rescue delay | TaskStep asset |
| Step order | TutorialSequence asset |
| Idle timeout/Attract message | TutorialConfig asset |
| New step | Existing Narration/Task/Choice type; do not add a fourth type |
| A-or-B completion | AnyOfCondition; do not add a one-off condition |
| New game fact | GameEventId + publisher/bus wiring |
| New game action | GameCommandId + single router case |

Step/Condition は shared ScriptableObject なので、実行中の進捗を field に保持しない。状態は TutorialContext または実行メソッドの local state に置く。

## Session Cancellation and Reset

```text
UserAbsent / Idle / StaffReset / ReturnToAttract
                    ↓
             cancel session token
                    ↓
      running step/load/effect exits
                    ↓
     reset with an independent token
                    ↓
                 Attract
```

Reset 対象:

- 表示中食品、食事残量、AABB/collider、crumb/effect。
- selected item、menu visibility、current loading/error。
- download/session context、QR recognition session state、idle watcher。
- Presenter visibility、TutorialContext blackboard、current step。

通常 reset で保持する対象:

- 生成食品 catalog/history と明示 policy に従う preview/model cache。
- 展示運営者が確定した Spatial Anchor UUID と anchor-relative food placement。

状態を持つ game feature を追加するときは、Session Reset Coordinator/GameResetter の対象または永続対象のどちらかを必ず決める。

## Diagnostics and Smoke Flow

- Debug HUD: AppState、step ID、elapsed seconds。Editor/Development Build で有効化し、本番は設定で制御する。
- Logs: Tutorial enter/exit/rescue、GameEvent、GameCommand、AppState、anchor、item selection、preview/model load、scoop/remaining portion、reset を相関可能にする。
- Test injection: Start、AnchorDesignated、FoodScooped、DishCleared、UserAbsent を手動発火できる test adapter を用意できる。

代表 smoke flow:

1. Start で Attract→Tutorial。
2. AnchorDesignated で S3 相当を通過。
3. 無操作時に hint→rescue が発生することを確認。
4. 前菜で scoop と clear を実 event から通過。
5. S14 後に scene reload/blackout なしで FreePlay。
6. Virtual Menu は一つの UI に v2 API item と Standalone local item を同時表示し、Network item は image/metadata だけを先行取得する。
7. Network/Standalone から各1 item を選び、対応 loader だけが動いて共通 anchor/eating flow へ到達することを確認する。
8. API を無効化しても Standalone item の選択・表示が継続することを確認する。
9. 途中で StaffReset を発火し、3秒以内に Attract、一時 food/UI が消え、catalog/placement が残ることを確認。

## Existing Adaptations That May Be Preserved

- TutorialRunner/SessionController が MonoBehaviour ではなく Extenject 管理の POCO であること。
- Presenter が async API に加えて reactive state を公開すること。
- Voice/Choice Presenter が追加されていること。
- S1 の Attract message を `TutorialConfig` が担当し、実行 step は S2 から始まること。

これらは責務分離、data-driven editing、同一 scene のシームレス移行を満たす限り、元仕様に対する許容 adaptation である。

## Known Migration Work

- QR GUID → food selection/download trigger を Catalog/Menu item selection へ置き換える。
- Network mode を YummyService v2 order/artifact client へ置き換え、廃止済み v1 API と旧 `/{guid}/model` を全 runtime/fallback から除去する。
- History/status/selected artifact metadata/preview/GLB/auth/problem/compatibility の v2 transport contract 公開を待ち、未定義 path を推測しない。
- S3 文言、condition/event ID を anchor designation semantics へ変更する。
- Generated Food Catalog、Preview Repository、Virtual Menu、Physical Viewer contract を追加する。
- 実 model geometry から AABB を作り、Scoop Detector、Consumption State、crumb/disappear を Game event へ接続する。
- Dummy FoodScooped/DishCleared の本番依存を除去する。
- `Q1`〜`Q5` を Unit ごとに解決してから実装合格を判定する。
