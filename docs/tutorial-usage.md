# YummyVerse チュートリアル コアロジック 使い方

`docs/tutorial-requirement.md` の実装。ここでは **コアロジックの使い方** を説明する。

---

## 0. まず全体像

```
ゲーム機能 ──(発火)──> IGameEventPublisher ─┐
                                            ├─> GameEventBus ──(購読)──> TutorialCondition
MRUK / InputLayer / QRDetectionService ─────┘                                   ↑
                                                                        TutorialStep (SO)
                                                                                ↑
IGameCommandBus <──(依頼)── TutorialStep                              TutorialSequence (SO)
       ↓                                                                        ↑
GameCommandRouter ──> 既存の FoodViewModel / StandaloneWindowViewModel     TutorialRunner
                                                                                ↑
                                                                        SessionController
```

依存は **一方向**。`TutorialRunner` はゲームのコンポーネントを一切参照しない。
ゲーム機能は AppState が `Tutorial` の間も常時稼働するので、チュートリアル終了時にゲームを起動し直す処理は存在しない。

| レイヤ | 実体 |
|---|---|
| AppState | `Model/AppStateMachine.cs` (Attract / Tutorial / FreePlay / Outro) |
| セッション | `ViewModel/Tutorial/SessionController.cs` |
| 進行 | `ViewModel/Tutorial/TutorialRunner.cs` + `TutorialSequence` アセット |
| ステップ | `ViewModel/Tutorial/SO/Steps/` (Narration / Task / Choice の3種のみ) |
| 完了条件 | `ViewModel/Tutorial/SO/Conditions/` (5種) |
| 表示 | `ViewModel/Tutorial/*Presenter.cs` + `View/Tutorial/*View.cs` |
| ゲーム機能 | 既存の `QRDetectionService` / `FoodPlacementService` / `FoodViewModel` / `StandaloneWindowViewModel` など |

---

## 1. セットアップ（初回のみ、Unity エディタでの作業）

コードだけでは完結しない部分。上から順に。

### 1-1. Localization の初期化

1. `Edit > Project Settings > Localization` を開き、**Localization Settings を作成**
2. Locale に **日本語 (ja)** を追加（英語も足すなら en も）

> `com.unity.localization` は `Packages/manifest.json` に追加済み。

### 1-2. アセットの自動生成

メニューから **`YummyVerse > Tutorial > Create Default Tutorial Assets`** を実行。
`Assets/YummyVerse/Data/Tutorial/` 以下に次が生成される。

```
Conditions/   Cond_ButtonPressed, Cond_Time3s, Cond_Time5s, Cond_Time8s,
              Cond_ButtonOr8s, Cond_QrPlateDetected, Cond_FoodScooped, Cond_DishCleared
Steps/        Step_S2_Welcome … Step_S19_Farewell
Sequences/    TutorialSequence_Main (S2〜S14) / TutorialSequence_FreePlay (S15〜S19)
Feedback/     SuccessFeedback_OK
Localization/ TutorialStrings (日本語の原文入り)
TutorialConfig.asset
```

**冪等**なので何度実行してもよい。既存アセットと既に入力済みの文言は上書きしない。

### 1-3. 入力アクションの反映

`Assets/YummyVerse/Scripts/InputActions/Restaurant.inputactions` に次を追加済み。
エディタで開いて **Apply**（`Restaurant.cs` の再生成）を一度行うこと。

| アクション | バインド | 用途 |
|---|---|---|
| `Eating/Start` | 右手トリガー / `Space` | 決定・スタートボタン |
| `Eating/StaffReset` | `F5` | スタッフ用の強制リセット |

### 1-4. シーンへの配置

`Restaurant` シーンで作業する。**チュートリアル専用シーンは作らない**（シームレス移行の要件）。

1. **`SceneContext.prefab` の `Mono Installers` に `TutorialInstaller` と `SharedViewModelInstaller` を追加**
2. `TutorialInstaller` の `Tutorial Config` に生成された `TutorialConfig.asset` を割り当て
3. `SceneContext` 配下（**プレハブの `GameObjectContext` の中ではない**）に UI 一式を置く

| GameObject | 付けるコンポーネント | 必要な参照 |
|---|---|---|
| TutorialMessage | `TutorialMessageView` + `CanvasGroupPanel` | TMP テキスト |
| TutorialHint | `TutorialHintView` + `CanvasGroupPanel` | TMP テキスト, `VideoPlayer` |
| TutorialFeedback | `TutorialFeedbackView` + `CanvasGroupPanel` + `AudioSource` | TMP テキスト, punch 対象の RectTransform |
| TutorialChoice | `TutorialChoiceView` + `CanvasGroupPanel` | TMP, ボタンのプレハブ, 並べる親 Transform |
| TutorialVoice | `TutorialVoiceView` + `AudioSource` | — |
| TutorialDebugHud | `TutorialDebugHudView` | TMP テキスト |
| GameCommandRouter | `GameCommandRouter` | 前菜の品目 (既定 Curry) |

> **重要**: Presenter は SceneContext にバインドされる。`GameObjectContext` のサブコンテナに View を置くと親から解決できず `ZenjectException` になる。

4. `MRUK.prefab` の `TrackableRemoved` UnityEvent に **`QRView.OnTrackableRemoved`** を接続（現在空欄。QR ロストの検知に必要）
5. すくい判定・完食判定が未実装の間は、Dummy 系オブジェクトに **`DummyGameEventView`** を追加

### 1-5. Spatial Anchorと食べ物位置の設定

1. Quest上で右コントローラーの `A` ボタンを押し、管理画面を開く。
2. 水色の設定用CubeをGripで掴み、展示基準にする位置へ移動する。
3. `Set / Update Spatial Anchor` を押す。保存完了の表示を確認する。
4. Spatial Anchorは動かさず、Cubeだけを食べ物の表示位置へ移動する。
5. `Lock Food Position` を押す。

QRのTransformは表示位置に使わない。Anchor UUIDとCubeのanchor-relative poseは端末へ保存され、次回起動時にload/localizeして復元される。復元できない場合、食品はワールド原点へ表示されず、管理画面から再設定する。

---

## 2. コアロジックの使い方

### 2-1. ゲーム機能側が触るもの — `IGameEventPublisher`

チュートリアルの存在を知る必要はない。ゲームで何かが成立したら発火するだけ。

```csharp
public class ScoopDetector : MonoBehaviour
{
    private IGameEventPublisher _publisher;

    [Inject] public void Construct(IGameEventPublisher publisher) => _publisher = publisher;

    private void OnScooped()
    {
        _publisher.PublishFoodScooped(); // これだけ
    }
}
```

**既存機能から自動で流れているもの**（追加コード不要）:

| イベント | 発火元 |
|---|---|
| `StartButtonPressed` | `IInputLayer.OnStartButtonPressed` |
| `QrPlateDetected` | `IQRDetectionService.OnChangeTransform`（再認識のたびに発火） |
| `QrPlateLost` | `IQRDetectionService.OnLost`（MRUK の `TrackableRemoved` 経由） |
| `MenuItemSelected` | `IStandaloneWindowViewModel.SpawnLocalFood`（`MenuSelectionBridge` が変換） |
| `UserAbsent` | `IdleWatcher`（無操作タイマー、既定90秒） |

**発火元が未実装**なもの: `FoodScooped`, `DishCleared`。
当面は `DummyGameEventView` から発火する（`1` / `2` キー、または Inspector のコンテキストメニュー）。
`DummyGameEventView` は既定で **Bボタンの食べ物破棄を暫定的に完食として扱う**。本実装が入ったらこのチェックを外すこと。

### 2-2. チュートリアル側が触るもの — `IGameEventBus`

購読専用。Condition から使うので、通常は直接触らない。

```csharp
bus.OnDishCleared += () => { ... };          // 仕様書どおりの event
await bus.GetStream(GameEventId.DishCleared).FirstAsync(ct); // Condition が使う形
```

### 2-3. ゲームに何かさせたいとき — `IGameCommandBus`

チュートリアルからゲームのメソッドを直接呼ぶのは禁止。コマンドを1件投げる。

```csharp
ctx.Commands.Request(GameCommandId.ServeAppetizer);
```

受け手は `View/Tutorial/GameCommandRouter.cs` だけ。ここが既存の ViewModel に委譲する。

| コマンド | 実際の動作 |
|---|---|
| `ServeAppetizer` | `StandaloneWindowViewModel.SpawnLocalFood(appetizer)` |
| `DestroyAllFood` | `FoodViewModel.RequestDestroyFood()` → 既存の `FoodView.TryDestroyFood()` |
| `ShowMenu` / `HideMenu` | `StandaloneWindowViewModel.IsVisible` |

コマンドを増やすときは `GameCommandId` に足して `GameCommandRouter.Handle` に1ケース書くだけ。

---

## 3. よくある作業

### 3-1. 文言・秒数・順序を変える → **コードは触らない**

| 変えたいもの | 触る場所 |
|---|---|
| 文言 | `Window > Asset Management > Localization Tables` の `TutorialStrings` |
| 待ち時間 | `Cond_Time*.asset` の `seconds` |
| ヒントが出るまで / 救済まで | 各 `Step_*.asset` の `hintDelaySeconds` / `rescueTimeoutSeconds` |
| ステップの順序・追加・削除 | `TutorialSequence_Main.asset` の `steps` リスト |
| 無操作タイムアウト | `TutorialConfig.asset` の `idleTimeoutSeconds` |
| Attract の文言（旧 S1） | `TutorialConfig.asset` の `attractMessage` |

### 3-2. ステップを1つ追加する

1. `Assets > Create > YummyVerse > Tutorial > Step > Narration`（または Task / Choice）
2. `stepId`（例 `S20`）、文言、完了条件を設定
3. `TutorialSequence_Main` の `steps` に挿入

**コードは1行も書かない。** ステップの具象型は Narration / Task / Choice の3種から増やさないこと。

### 3-3. 新しい完了条件が必要になったら

`TutorialCondition` を継承して1メソッド書くだけ。

```csharp
[CreateAssetMenu(fileName = "Cond_", menuName = "YummyVerse/Tutorial/Condition/Two Hands")]
public class TwoHandsCondition : TutorialCondition
{
    [SerializeField] private float holdSeconds = 2f;

    public override async UniTask WaitAsync(TutorialContext ctx, CancellationToken ct)
    {
        await UniTask.WaitUntil(() => /* 判定 */, cancellationToken: ct);
        await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken: ct);
    }
}
```

> **必ず守ること**: `TutorialCondition` は ScriptableObject＝アセットが複数ステップから共有される。
> 待機中の進捗をフィールドに持たせてはいけない。状態はすべてローカル変数で扱う。

「A または B」は新クラスを作らず `AnyOfCondition` で表現する。

### 3-4. 新しいゲームイベントを増やす

1. `GameEventId` に値を追加
2. `GameEventBus.Initialize()` で既存の何かに配線する（自動発火できるなら）
   　できないなら `IGameEventPublisher` にメソッドを1本足してゲーム側から呼ぶ
3. `Cond_Event_*` アセットを作って `eventId` に選ぶだけで、すぐステップから使える

### 3-5. 救済ポリシーの選び方

TaskStep は詰まりを前提に3段階で扱う。

1. `hintDelaySeconds` 経過 → ヒント提示（テキスト + デモ動画ループ）
2. `rescueTimeoutSeconds` 経過 → `rescuePolicy` に従う
3. 各段階を `ITutorialAnalytics` に記録（既定は `Debug.Log`。外部送信が要るなら差し替える）

| ポリシー | 使いどころ |
|---|---|
| `AutoAdvance`（既定） | 理解を諦めて次へ。**迷ったらこれ** |
| `ForceComplete` | ゲーム側に完了を代行させる（例: S11 は食べ物を強制的に消す） |
| `ReturnToAttract` | そのステップを越えられないなら体験を打ち切る |

**来場者を絶対に立ち往生させないこと。** `rescueTimeoutSeconds` のない TaskStep を作ってはいけない。

---

## 4. 中断とリセット（展示運用の要）

中断は **`CancellationToken` の一括伝播だけ**で処理する。ステップごとに Attract への戻り線は書かない。

```
中断トリガー ──> SessionController.AbortSession()
                        ↓
                _sessionCts.Cancel()
                        ↓
        実行中の await がすべて OperationCanceledException
                        ↓
                finally { ResetToAttractAsync() }   ← 必ず通る
```

中断トリガーは3つ:

| トリガー | 経路 |
|---|---|
| 無操作／人検知ロスト | `IGameEventBus.OnUserAbsent` |
| スタッフ用リセット | `IInputLayer.OnStaffResetPressed`（F5） |
| 救済 `ReturnToAttract` | `TutorialContext.RequestAbort()` |

`ResetToAttractAsync()` は意図的に `CancellationToken.None` で動く。中断のキャンセルに巻き込まれてリセットが中途半端に終わらないようにするため。

### `GameResetter` への追記が必要になるケース

**状態を持つゲーム機能を追加したら、必ず `Model/GameResetter.cs` にリセット処理を足す。**
ここに漏れがあると2人目の来場者で破綻する。現在は次を初期化している。

- 皿の上の食品 → `GameCommandId.DestroyAllFood`
- メニューUI → `GameCommandId.HideMenu`
- ダウンロード結果 → `IFoodContext.Reset()`
- QR認識状態 → `IQRDetectionService.Reset()`
- 無操作監視 → `IIdleWatcher.SetActive(false)`

Spatial Anchorと固定済みの食べ物位置は来場者ごとの状態ではないため、このリセット対象へ追加しない。再設定は管理画面の `Set / Update Spatial Anchor` と `Lock Food Position` で明示的に行う。

加えて `SessionController.ResetToAttractAsync()` が全 Presenter を隠し、
`TutorialContext.ResetForNewSession()` が Blackboard と `IsFirstTimeUser` を、
`IGameEventPublisher.ResetSessionState()` が直近の選択を捨てる。

---

## 5. デバッグ

- **デバッグHUD**: `TutorialDebugHudView` が AppState / ステップID / 経過秒を表示。
  エディタと Development Build で自動的に有効。本番でも見たいときは `forceEnable` にチェック。
- **ログ**: すべて接頭辞つきで出る。
  `[Tutorial] Enter S3` / `[Tutorial] Exit S3 (4.2s)` / `[GameEvent] QrPlateDetected` /
  `[GameCommand] ServeAppetizer` / `[AppState] Attract -> Tutorial` / `[TutorialAnalytics] S3 Rescued (30.0s)`
- **手動でイベントを発火**: `DummyGameEventView`（`1`=すくい, `2`=完食, `3`=UserAbsent）
- **QR を偽装**: 既存の `DummyQRView.FakeQRDetect()`
- **食べ物を出す**: Standalone Mode を ON にして `StandaloneWindowView` のボタン

### 通し確認の手順（Editor Play Mode, Standalone Mode ON）

1. `Space` で Attract → Tutorial。HUD に `S2` が出る
2. `DummyQRView.FakeQRDetect()` で S3 が成功し「OK!」が出る
3. S3 で **何もしない** → 5秒でヒント、30秒で AutoAdvance（無限に止まらないことの確認）
4. `1` / `2` キーで S8 / S11 を通過
5. S14 の後、暗転もロードもなく AppState が `FreePlay` になる
6. 途中で `F5` → 3秒以内に `Attract`、HUD のステップIDが `-`、皿の食品が消えている

---

## 6. 仕様書からの差分（意図的なもの）

| 仕様書 | 実装 | 理由 |
|---|---|---|
| `TutorialRunner` / `SessionController` は `MonoBehaviour` | 素の C# クラス（Zenject `IInitializable`） | 本プロジェクトはロジックを POCO、MonoBehaviour は View 専用という既存規約に合わせた |
| Presenter は `ShowAsync` / `HideAsync` のみ | `ReactiveProperty` も公開 | 既存の `IConfigUIViewModel` と同じ「状態は ReactiveProperty、コマンドはメソッド」の形。ステップが UI を直接触らない点は変わらない |
| Presenter は3つ | `IVoicePresenter` / `IChoicePresenter` を追加 | `NarrationStep.voiceClip` と `ChoiceStep` に受け皿が無かったため。置換ではなく追加 |
| S1 を Narration ステップとして持つ | `TutorialConfig.attractMessage` として Attract 状態が担当 | 「来場者を待つ」のは `AppState.Attract` の責務。ここをステップにすると、誰もいない間 AppState が `Tutorial` になり無操作監視とも噛み合わない。文言は SO で編集可能なので、データ駆動の要件は満たしている |

既存コードへの変更は、いずれも**追加のみ**で挙動は変えていない。

- `IInputLayer` に `OnStartButtonPressed` / `OnStaffResetPressed`
- `IQRDetectionService` に `OnLost` / `NotifyLostQR()` / `Reset()`
- `IFoodContext` に `Reset()`
- `IFoodViewModel` に `RequestDestroyFood()`
- `IStandaloneWindowViewModel` に `OnLocalFoodSpawned`
- `IQRViewModel` / `QRView` に `HandleTrackableRemoved` / `OnTrackableRemoved`

1点だけ DI の構成を変えている: `FoodViewModel` と `StandaloneWindowViewModel` のバインドを
プレハブの `GameObjectContext`（`FoodInstaller` / `ConfigUIInstaller`）から
シーンスコープの `SharedViewModelInstaller` へ引き上げた。
サブコンテナのバインドは親コンテナから解決できないため、チュートリアル層から参照できなかったのが理由。
プレハブ内の View はサブコンテナが親を辿るので、これまでどおり同じインスタンスを受け取る。

---

## 7. やってはいけないこと（レビューで差し戻す）

- ステップの種類ごとに enum を増やして `switch` で分岐する
- 「メッセージ表示中」を状態として保持する（表示はステップの副作用であって状態ではない）
- `TutorialRunner` からゲームのコンポーネントを直接呼ぶ（必ず `IGameCommandBus` 経由）
- ステップから任意の別ステップへジャンプする（分岐は `ChoiceStep` のサブシーケンスのみ）
- チュートリアル専用シーンを分ける
- 救済タイムアウトのない `TaskStep`
- `TutorialCondition` / `TutorialStep` に実行時の状態フィールドを持たせる
