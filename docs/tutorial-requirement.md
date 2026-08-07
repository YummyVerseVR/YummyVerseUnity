# YummyVerse チュートリアル コアロジック 実装指示書

## 0. このドキュメントの目的

展示型プロダクト YummyVerse のチュートリアル進行を、**データ駆動のステップ列**として実装するための仕様。
実装対象は「チュートリアル進行のコアロジック」のみ。QR認識・すくい判定・完食判定などのゲーム機能そのものは対象外で、既存（または並行開発中）の実装をイベント経由で購読する。

---

## 1. 設計方針（必読・逸脱禁止）

### 1.1 ステップの正規化

チュートリアルの全ステップを、以下の統一形に落とす。

```
1ステップ = 提示(Present) → 完了条件の待機(Await) → 成功演出(Succeed)
```

「説明文だけ出す状態」と「ユーザー操作を待つ状態」を別の型にしない。
前者は「完了条件が時間経過またはボタン入力であるステップ」に過ぎない。

### 1.2 レイヤ分離

| レイヤ | 責務 | 実体 |
|---|---|---|
| AppState | アプリ全体の粗いモード管理 | 有限状態機械（4状態） |
| TutorialSequence | チュートリアルの進行順序 | ScriptableObject のステップ列 |
| Presenter | 画面表示・音声・演出 | インターフェース経由 |
| Game | QR認識、すくい判定、完食判定など | 既存実装（イベント発火のみ担当） |

### 1.3 依存方向

```
Game ──(イベント発火)──> EventBus ──(購読)──> TutorialRunner ──> Presenter
```

**一方向厳守。** TutorialRunner から Game のロジックを直接呼び出して駆動してはならない。
ゲーム機能は AppState が `Tutorial` の間も常時稼働させ、チュートリアルは「教える層」に徹する。
これによりチュートリアル終了時に**ゲームを起動し直す処理が一切不要**になり、シームレスに実体験へ移行できる。

---

## 2. 前提環境

- Unity 6 (`Awaitable`) または UniTask v2 を使用。以下のコードは `UniTask` 表記だが、`Awaitable` に読み替え可。
- `async/await` ベースで実装する。**ステップごとの enum + switch による巨大FSMは禁止**（状態爆発するため）。
- `CancellationToken` を全非同期処理に貫通させる。

---

## 3. 型定義

### 3.1 AppState

```csharp
public enum AppState
{
    Attract,   // 待機・アトラクトループ。来場者を待つ
    Tutorial,  // チュートリアル進行中
    FreePlay,  // 自由体験（注文〜完食）
    Outro      // 締めの表示 → Attract へ戻る
}
```

- 遷移は一方向 + `Attract` への強制復帰のみ。
- **チュートリアルの内部進行を AppState に持ち込まないこと。**

### 3.2 ゲームイベント契約

ゲーム側が発火し、チュートリアルが購読する。**チュートリアルの都合をゲーム側に漏らさない。**

```csharp
public interface IGameEventBus
{
    event Action OnStartButtonPressed;
    event Action OnQrPlateDetected;      // 紙皿のQR認識成立
    event Action OnQrPlateLost;          // 認識ロスト
    event Action OnFoodScooped;          // 1回すくった
    event Action OnDishCleared;          // 完食
    event Action<MenuItem> OnMenuItemSelected;
    event Action OnUserAbsent;           // 一定時間の無操作／人検知ロスト
}
```

発火責任はゲーム側。チュートリアルは購読のみ。

### 3.3 完了条件

```csharp
public abstract class TutorialCondition : ScriptableObject
{
    /// <summary>条件成立まで待機する。キャンセル時は OperationCanceledException。</summary>
    public abstract UniTask WaitAsync(TutorialContext ctx, CancellationToken ct);
}
```

実装クラス（最低限これらを用意）：

| クラス | 成立条件 |
|---|---|
| `ButtonPressedCondition` | 決定ボタン押下 |
| `TimeElapsedCondition` | 指定秒数の経過（`seconds` フィールド） |
| `GameEventCondition` | 指定した `GameEventId` の発火 |
| `CountedEventCondition` | 指定イベントが n 回発火 |
| `AnyOfCondition` | 子条件のいずれかが成立（複合用） |

`AnyOfCondition` を用意しておくことで「ボタン押下 **または** 5秒経過で進む」を新クラスなしで表現できる。

### 3.4 ステップ

```csharp
public abstract class TutorialStep : ScriptableObject
{
    [SerializeField] private string stepId;       // "S1", "S3.1" など。ログ・デバッグ用
    [SerializeField] private bool skippableOnRepeat; // 2周目以降スキップするか

    public string StepId => stepId;
    public bool SkippableOnRepeat => skippableOnRepeat;

    public abstract UniTask ExecuteAsync(TutorialContext ctx, CancellationToken ct);
}
```

具象は **3種類のみ**。これ以上増やさない。

#### NarrationStep

提示して、条件成立で次へ。成功演出なし。

```
フィールド: message(LocalizedString), voiceClip(optional),
           completionCondition(TutorialCondition)
処理:      Presenter.ShowMessage(message)
           → completionCondition.WaitAsync()
```

対応：S1, S2, S5, S6, S8, S11, S14, S15, S18, S19

#### TaskStep

指示を出し、ゲームイベントの達成を待つ。滞留したらヒントを強化し、それでも進まなければ救済する。

```
フィールド: instruction(LocalizedString),
           successCondition(TutorialCondition),
           hint(HintPresentation: テキスト／デモ動画／両方),
           hintDelaySeconds(default 5),
           rescueTimeoutSeconds(default 30),
           rescuePolicy(enum: AutoAdvance / ForceComplete / ReturnToAttract),
           successFeedback(SuccessFeedbackAsset)
処理:      instruction 提示
           → hintDelay 経過でヒント提示（デモ動画ループ等）
           → successCondition 成立 or rescueTimeout
           → successFeedback 再生（「OK!」の大きめ表示）
```

対応：S3+S3.1, S8+S9, S11+S12（**指示と催促を1ステップに統合すること**）

> S4 / S10 / S13 の「OK!」は独立ステップにせず、TaskStep の `successFeedback` として共通アセット化する。

#### ChoiceStep

分岐。

```
フィールド: prompt(LocalizedString), options(List<ChoiceOption>),
           timeoutSeconds, defaultOptionIndex
処理:      選択肢提示 → 選択 or タイムアウトで既定値
戻り値:    TutorialContext に選択結果を格納し、Sequence 側が分岐先を決定
```

対応：S6の初回判定、S16のメニュー表示

### 3.5 シーケンス

```csharp
[CreateAssetMenu(menuName = "YummyVerse/Tutorial Sequence")]
public class TutorialSequence : ScriptableObject
{
    [SerializeField] private List<TutorialStep> steps;
    public IReadOnlyList<TutorialStep> Steps => steps;
}
```

分岐は `ChoiceStep` の結果でサブシーケンスを差し替える形にし、**ステップ間に任意のジャンプを持たせない**（デバッグ不能になるため）。

### 3.6 コンテキスト

```csharp
public class TutorialContext
{
    public IGameEventBus Events { get; }
    public IMessagePresenter Message { get; }
    public IHintPresenter Hint { get; }
    public IFeedbackPresenter Feedback { get; }
    public bool IsFirstTimeUser { get; set; }
    public IDictionary<string, object> Blackboard { get; } // 選択結果等
}
```

### 3.7 プレゼンタ

表示は必ずインターフェース越し。ステップが UI コンポーネントを直接触ることを禁止する。

```csharp
public interface IMessagePresenter
{
    UniTask ShowAsync(LocalizedString msg, CancellationToken ct);
    UniTask HideAsync(CancellationToken ct);
}

public interface IHintPresenter
{
    UniTask ShowAsync(HintPresentation hint, CancellationToken ct); // 動画ループ含む
    UniTask HideAsync(CancellationToken ct);
}

public interface IFeedbackPresenter
{
    UniTask PlaySuccessAsync(SuccessFeedbackAsset asset, CancellationToken ct);
}
```

---

## 4. ランナー

```csharp
public class TutorialRunner : MonoBehaviour
{
    public async UniTask RunAsync(TutorialSequence sequence, TutorialContext ctx, CancellationToken ct)
    {
        foreach (var step in sequence.Steps)
        {
            if (!ctx.IsFirstTimeUser && step.SkippableOnRepeat) continue;

            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Log($"[Tutorial] Enter {step.StepId}");
            await step.ExecuteAsync(ctx, stepCts.Token);
            Log($"[Tutorial] Exit  {step.StepId}");
        }
    }
}
```

要件：

- 各ステップの入退場を `stepId` 付きでログ出力（展示現場でのトラブルシュート用）。
- 現在のステップIDを外部から読める公開プロパティを持たせる（デバッグHUD用）。
- 例外時は握り潰さず、上位のセッション管理へ伝播させる。

---

## 5. 中断とリセット（展示運用の要）

**最重要要件。** 来場者は途中で必ず離脱する。

```csharp
public class SessionController : MonoBehaviour
{
    private CancellationTokenSource _sessionCts;

    public async UniTask RunSessionAsync()
    {
        _sessionCts = new CancellationTokenSource();
        try
        {
            await _runner.RunAsync(_tutorialSequence, _ctx, _sessionCts.Token);
            await _freePlay.RunAsync(_ctx, _sessionCts.Token);
        }
        catch (OperationCanceledException)
        {
            // 想定内。中断演出へ
        }
        finally
        {
            await ResetToAttractAsync();
        }
    }

    public void AbortSession() => _sessionCts?.Cancel();
}
```

- 中断トリガー：`OnUserAbsent`、グローバル無操作タイムアウト、スタッフ用リセット入力（キーボードショートカット等を必ず用意）。
- **ステップごとに Attract への戻り線を書かない。** キャンセルの一括伝播で処理する。
- `ResetToAttractAsync()` はゲーム側の状態（皿上の食品、注文内容、認識状態）も初期化する。ここに漏れがあると2人目の来場者で破綻する。

### 救済ポリシー

各 TaskStep は詰まりを前提に3段階で扱う。

1. `hintDelaySeconds` 経過 → ヒント提示（テキスト強調 + デモ動画ループ）
2. `rescueTimeoutSeconds` 経過 → `rescuePolicy` に従う
   - `AutoAdvance`: 達成扱いにせず次へ進む（説明の理解を諦める）
   - `ForceComplete`: ゲーム側に完了を代行させる（例：食品を強制的に消す）
   - `ReturnToAttract`: セッションを中断
3. 各段階への遷移をアナリティクスに記録（どのステップで詰まるかを後で改善するため）

デフォルトは `AutoAdvance`。**来場者を絶対に立ち往生させないこと。**

---

## 6. ステップ定義データ

以下を ScriptableObject アセットとして作成する（コードにハードコードしない）。

| ID | 型 | 内容 | 完了条件 |
|---|---|---|---|
| S1 | Narration | ボタンを押してスタート | ボタン |
| S2 | Narration | YummyVerse へようこそ／AI生成の食感体験の紹介 | ボタン or 時間 |
| S3 | **Task** | 紙皿を見つめよう（S3.1の催促を統合） | QR認識 |
| S5 | Narration | AIシェフの準備ができたようです | 時間 |
| S6 | Choice | 初回かどうかの判定 | 選択 or タイムアウト |
| S6' | Narration | 前菜のリンゴを食べてみましょう | ボタン |
| S7 | — | リンゴを皿に表示（**ゲーム側の処理**。Narration の副作用ではなくイベント送出で依頼） | — |
| S8 | **Task** | スプーンですくいます（S9の催促・デモ動画を統合） | すくわれた |
| S11 | **Task** | このまま完食しよう（S12の催促を統合） | 完食した |
| S14 | Narration | 食事作法はお分かりいただけましたか | 時間 |
| S15 | Narration | 食べたいモノを注文してみましょう | ボタン |
| S16 | Choice | メニュー表示 | 選択 |
| S17 | — | 選択された食品を提供（ゲーム側） | — |
| S18 | Narration | 完食ありがとうございました | 完食 |
| S19 | Narration | またのご来店をお待ちしております | 時間 → Attract |

- S15 以降は **FreePlay として実装**し、チュートリアルシーケンスに含めない。境界は S14 終了時点。
- S7 / S17 のような「ゲームに何かさせる」処理は、`GameCommand` を1件発行する専用ステップ型を作らず、Narration の完了時コールバックまたは FreePlay 側の責務とする。

---

## 7. 受け入れ基準

- [ ] チュートリアルの全ステップが ScriptableObject として編集可能で、コード変更なしに文言・秒数・順序を差し替えられる
- [ ] 任意のステップ実行中に中断入力を与えると、3秒以内に Attract へ戻り、ゲーム状態も初期化されている
- [ ] TaskStep で何も操作しない場合、ヒント提示 → 救済の順に自動進行し、無限に停止しない
- [ ] 連続10セッションを操作なし／途中離脱／正常完走の混在で回してもリークやステート汚染が起きない
- [ ] チュートリアル完走から FreePlay への移行で、シーン遷移・ロード・暗転が発生しない
- [ ] 現在のステップIDと経過秒数を表示するデバッグHUDがある（本番ビルドでは無効化可能）

---

## 8. アンチパターン（レビューで差し戻す対象）

- ステップの種類ごとに enum を増やし、`switch` で分岐する実装
- 「メッセージ表示中」を状態として保持する実装（表示はステップの副作用であって状態ではない）
- TutorialRunner がゲームのコンポーネントを直接参照して機能を呼び出す実装
- ステップから任意の別ステップへジャンプする仕組み
- チュートリアル専用シーンを分ける実装（シームレス移行の要件に反する）
- 救済タイムアウトのない TaskSte# YummyVerse チュートリアル コアロジック 実装指示書

## 0. このドキュメントの目的

展示型プロダクト YummyVerse のチュートリアル進行を、**データ駆動のステップ列**として実装するための仕様。
実装対象は「チュートリアル進行のコアロジック」のみ。QR認識・すくい判定・完食判定などのゲーム機能そのものは対象外で、既存（または並行開発中）の実装をイベント経由で購読する。

---

## 1. 設計方針（必読・逸脱禁止）

### 1.1 ステップの正規化

チュートリアルの全ステップを、以下の統一形に落とす。

```
1ステップ = 提示(Present) → 完了条件の待機(Await) → 成功演出(Succeed)
```

「説明文だけ出す状態」と「ユーザー操作を待つ状態」を別の型にしない。
前者は「完了条件が時間経過またはボタン入力であるステップ」に過ぎない。

### 1.2 レイヤ分離

| レイヤ | 責務 | 実体 |
|---|---|---|
| AppState | アプリ全体の粗いモード管理 | 有限状態機械（4状態） |
| TutorialSequence | チュートリアルの進行順序 | ScriptableObject のステップ列 |
| Presenter | 画面表示・音声・演出 | インターフェース経由 |
| Game | QR認識、すくい判定、完食判定など | 既存実装（イベント発火のみ担当） |

### 1.3 依存方向

```
Game ──(イベント発火)──> EventBus ──(購読)──> TutorialRunner ──> Presenter
```

**一方向厳守。** TutorialRunner から Game のロジックを直接呼び出して駆動してはならない。
ゲーム機能は AppState が `Tutorial` の間も常時稼働させ、チュートリアルは「教える層」に徹する。
これによりチュートリアル終了時に**ゲームを起動し直す処理が一切不要**になり、シームレスに実体験へ移行できる。

---

## 2. 前提環境

- Unity 6 (`Awaitable`) または UniTask v2 を使用。以下のコードは `UniTask` 表記だが、`Awaitable` に読み替え可。
- `async/await` ベースで実装する。**ステップごとの enum + switch による巨大FSMは禁止**（状態爆発するため）。
- `CancellationToken` を全非同期処理に貫通させる。

---

## 3. 型定義

### 3.1 AppState

```csharp
public enum AppState
{
    Attract,   // 待機・アトラクトループ。来場者を待つ
    Tutorial,  // チュートリアル進行中
    FreePlay,  // 自由体験（注文〜完食）
    Outro      // 締めの表示 → Attract へ戻る
}
```

- 遷移は一方向 + `Attract` への強制復帰のみ。
- **チュートリアルの内部進行を AppState に持ち込まないこと。**

### 3.2 ゲームイベント契約

ゲーム側が発火し、チュートリアルが購読する。**チュートリアルの都合をゲーム側に漏らさない。**

```csharp
public interface IGameEventBus
{
    event Action OnStartButtonPressed;
    event Action OnQrPlateDetected;      // 紙皿のQR認識成立
    event Action OnQrPlateLost;          // 認識ロスト
    event Action OnFoodScooped;          // 1回すくった
    event Action OnDishCleared;          // 完食
    event Action<MenuItem> OnMenuItemSelected;
    event Action OnUserAbsent;           // 一定時間の無操作／人検知ロスト
}
```

発火責任はゲーム側。チュートリアルは購読のみ。

### 3.3 完了条件

```csharp
public abstract class TutorialCondition : ScriptableObject
{
    /// <summary>条件成立まで待機する。キャンセル時は OperationCanceledException。</summary>
    public abstract UniTask WaitAsync(TutorialContext ctx, CancellationToken ct);
}
```

実装クラス（最低限これらを用意）：

| クラス | 成立条件 |
|---|---|
| `ButtonPressedCondition` | 決定ボタン押下 |
| `TimeElapsedCondition` | 指定秒数の経過（`seconds` フィールド） |
| `GameEventCondition` | 指定した `GameEventId` の発火 |
| `CountedEventCondition` | 指定イベントが n 回発火 |
| `AnyOfCondition` | 子条件のいずれかが成立（複合用） |

`AnyOfCondition` を用意しておくことで「ボタン押下 **または** 5秒経過で進む」を新クラスなしで表現できる。

### 3.4 ステップ

```csharp
public abstract class TutorialStep : ScriptableObject
{
    [SerializeField] private string stepId;       // "S1", "S3.1" など。ログ・デバッグ用
    [SerializeField] private bool skippableOnRepeat; // 2周目以降スキップするか

    public string StepId => stepId;
    public bool SkippableOnRepeat => skippableOnRepeat;

    public abstract UniTask ExecuteAsync(TutorialContext ctx, CancellationToken ct);
}
```

具象は **3種類のみ**。これ以上増やさない。

#### NarrationStep

提示して、条件成立で次へ。成功演出なし。

```
フィールド: message(LocalizedString), voiceClip(optional),
           completionCondition(TutorialCondition)
処理:      Presenter.ShowMessage(message)
           → completionCondition.WaitAsync()
```

対応：S1, S2, S5, S6, S8, S11, S14, S15, S18, S19

#### TaskStep

指示を出し、ゲームイベントの達成を待つ。滞留したらヒントを強化し、それでも進まなければ救済する。

```
フィールド: instruction(LocalizedString),
           successCondition(TutorialCondition),
           hint(HintPresentation: テキスト／デモ動画／両方),
           hintDelaySeconds(default 5),
           rescueTimeoutSeconds(default 30),
           rescuePolicy(enum: AutoAdvance / ForceComplete / ReturnToAttract),
           successFeedback(SuccessFeedbackAsset)
処理:      instruction 提示
           → hintDelay 経過でヒント提示（デモ動画ループ等）
           → successCondition 成立 or rescueTimeout
           → successFeedback 再生（「OK!」の大きめ表示）
```

対応：S3+S3.1, S8+S9, S11+S12（**指示と催促を1ステップに統合すること**）

> S4 / S10 / S13 の「OK!」は独立ステップにせず、TaskStep の `successFeedback` として共通アセット化する。

#### ChoiceStep

分岐。

```
フィールド: prompt(LocalizedString), options(List<ChoiceOption>),
           timeoutSeconds, defaultOptionIndex
処理:      選択肢提示 → 選択 or タイムアウトで既定値
戻り値:    TutorialContext に選択結果を格納し、Sequence 側が分岐先を決定
```

対応：S6の初回判定、S16のメニュー表示

### 3.5 シーケンス

```csharp
[CreateAssetMenu(menuName = "YummyVerse/Tutorial Sequence")]
public class TutorialSequence : ScriptableObject
{
    [SerializeField] private List<TutorialStep> steps;
    public IReadOnlyList<TutorialStep> Steps => steps;
}
```

分岐は `ChoiceStep` の結果でサブシーケンスを差し替える形にし、**ステップ間に任意のジャンプを持たせない**（デバッグ不能になるため）。

### 3.6 コンテキスト

```csharp
public class TutorialContext
{
    public IGameEventBus Events { get; }
    public IMessagePresenter Message { get; }
    public IHintPresenter Hint { get; }
    public IFeedbackPresenter Feedback { get; }
    public bool IsFirstTimeUser { get; set; }
    public IDictionary<string, object> Blackboard { get; } // 選択結果等
}
```

### 3.7 プレゼンタ

表示は必ずインターフェース越し。ステップが UI コンポーネントを直接触ることを禁止する。

```csharp
public interface IMessagePresenter
{
    UniTask ShowAsync(LocalizedString msg, CancellationToken ct);
    UniTask HideAsync(CancellationToken ct);
}

public interface IHintPresenter
{
    UniTask ShowAsync(HintPresentation hint, CancellationToken ct); // 動画ループ含む
    UniTask HideAsync(CancellationToken ct);
}

public interface IFeedbackPresenter
{
    UniTask PlaySuccessAsync(SuccessFeedbackAsset asset, CancellationToken ct);
}
```

---

## 4. ランナー

```csharp
public class TutorialRunner : MonoBehaviour
{
    public async UniTask RunAsync(TutorialSequence sequence, TutorialContext ctx, CancellationToken ct)
    {
        foreach (var step in sequence.Steps)
        {
            if (!ctx.IsFirstTimeUser && step.SkippableOnRepeat) continue;

            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Log($"[Tutorial] Enter {step.StepId}");
            await step.ExecuteAsync(ctx, stepCts.Token);
            Log($"[Tutorial] Exit  {step.StepId}");
        }
    }
}
```

要件：

- 各ステップの入退場を `stepId` 付きでログ出力（展示現場でのトラブルシュート用）。
- 現在のステップIDを外部から読める公開プロパティを持たせる（デバッグHUD用）。
- 例外時は握り潰さず、上位のセッション管理へ伝播させる。

---

## 5. 中断とリセット（展示運用の要）

**最重要要件。** 来場者は途中で必ず離脱する。

```csharp
public class SessionController : MonoBehaviour
{
    private CancellationTokenSource _sessionCts;

    public async UniTask RunSessionAsync()
    {
        _sessionCts = new CancellationTokenSource();
        try
        {
            await _runner.RunAsync(_tutorialSequence, _ctx, _sessionCts.Token);
            await _freePlay.RunAsync(_ctx, _sessionCts.Token);
        }
        catch (OperationCanceledException)
        {
            // 想定内。中断演出へ
        }
        finally
        {
            await ResetToAttractAsync();
        }
    }

    public void AbortSession() => _sessionCts?.Cancel();
}
```

- 中断トリガー：`OnUserAbsent`、グローバル無操作タイムアウト、スタッフ用リセット入力（キーボードショートカット等を必ず用意）。
- **ステップごとに Attract への戻り線を書かない。** キャンセルの一括伝播で処理する。
- `ResetToAttractAsync()` はゲーム側の状態（皿上の食品、注文内容、認識状態）も初期化する。ここに漏れがあると2人目の来場者で破綻する。

### 救済ポリシー

各 TaskStep は詰まりを前提に3段階で扱う。

1. `hintDelaySeconds` 経過 → ヒント提示（テキスト強調 + デモ動画ループ）
2. `rescueTimeoutSeconds` 経過 → `rescuePolicy` に従う
   - `AutoAdvance`: 達成扱いにせず次へ進む（説明の理解を諦める）
   - `ForceComplete`: ゲーム側に完了を代行させる（例：食品を強制的に消す）
   - `ReturnToAttract`: セッションを中断
3. 各段階への遷移をアナリティクスに記録（どのステップで詰まるかを後で改善するため）

デフォルトは `AutoAdvance`。**来場者を絶対に立ち往生させないこと。**

---

## 6. ステップ定義データ

以下を ScriptableObject アセットとして作成する（コードにハードコードしない）。

| ID | 型 | 内容 | 完了条件 |
|---|---|---|---|
| S1 | Narration | ボタンを押してスタート | ボタン |
| S2 | Narration | YummyVerse へようこそ／AI生成の食感体験の紹介 | ボタン or 時間 |
| S3 | **Task** | 紙皿を見つめよう（S3.1の催促を統合） | QR認識 |
| S5 | Narration | AIシェフの準備ができたようです | 時間 |
| S6 | Choice | 初回かどうかの判定 | 選択 or タイムアウト |
| S6' | Narration | 前菜のリンゴを食べてみましょう | ボタン |
| S7 | — | リンゴを皿に表示（**ゲーム側の処理**。Narration の副作用ではなくイベント送出で依頼） | — |
| S8 | **Task** | スプーンですくいます（S9の催促・デモ動画を統合） | すくわれた |
| S11 | **Task** | このまま完食しよう（S12の催促を統合） | 完食した |
| S14 | Narration | 食事作法はお分かりいただけましたか | 時間 |
| S15 | Narration | 食べたいモノを注文してみましょう | ボタン |
| S16 | Choice | メニュー表示 | 選択 |
| S17 | — | 選択された食品を提供（ゲーム側） | — |
| S18 | Narration | 完食ありがとうございました | 完食 |
| S19 | Narration | またのご来店をお待ちしております | 時間 → Attract |

- S15 以降は **FreePlay として実装**し、チュートリアルシーケンスに含めない。境界は S14 終了時点。
- S7 / S17 のような「ゲームに何かさせる」処理は、`GameCommand` を1件発行する専用ステップ型を作らず、Narration の完了時コールバックまたは FreePlay 側の責務とする。

---

## 7. 受け入れ基準

- [ ] チュートリアルの全ステップが ScriptableObject として編集可能で、コード変更なしに文言・秒数・順序を差し替えられる
- [ ] 任意のステップ実行中に中断入力を与えると、3秒以内に Attract へ戻り、ゲーム状態も初期化されている
- [ ] TaskStep で何も操作しない場合、ヒント提示 → 救済の順に自動進行し、無限に停止しない
- [ ] 連続10セッションを操作なし／途中離脱／正常完走の混在で回してもリークやステート汚染が起きない
- [ ] チュートリアル完走から FreePlay への移行で、シーン遷移・ロード・暗転が発生しない
- [ ] 現在のステップIDと経過秒数を表示するデバッグHUDがある（本番ビルドでは無効化可能）

---

## 8. アンチパターン（レビューで差し戻す対象）

- ステップの種類ごとに enum を増やし、`switch` で分岐する実装
- 「メッセージ表示中」を状態として保持する実装（表示はステップの副作用であって状態ではない）
- TutorialRunner がゲームのコンポーネントを直接参照して機能を呼び出す実装
- ステップから任意の別ステップへジャンプする仕組み
- チュートリアル専用シーンを分ける実装（シームレス移行の要件に反する）
- 救済タイムアウトのない TaskStepp
