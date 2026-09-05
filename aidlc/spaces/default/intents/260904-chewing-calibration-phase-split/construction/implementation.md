# Construction: Implementation and Progress

## Status

`CODE-COMPLETE-PENDING-RUNTIME-VERIFICATION`。プロトコル v1.1 のフェーズ分割に対応するコード変更と、それに対応する asset 変更を入れた。C# compile は Editor 上で通過し、`CAL_START` → `CAL_ACCEPTED` → カウントダウン → `CAL_NOISE` までの動作を Play Mode で観測した。咀嚼計を装着した状態での通し確認と EditMode テスト実行は未実施であり (`verification/test-results.md` を参照)、intent の completion は宣言しない。

## Implemented Design

```text
ChewingCalibrationFlow (Presentation)
        implements IChewingCalibrationPrompt  ──┐
                                                │ 案内とカウントダウンの待ち合わせ
ChewingSensorService (Model) ───────────────────┘
        CAL_START → CAL_ACCEPTED
        → prompt.PrepareAsync(Noise) → CAL_NOISE  → CAL_NOISE_DONE
        → prompt.PrepareAsync(Chew)  → CAL_CHEW   → CAL_CHEW_DONE → CAL_DONE
```

フェーズの順序、再送、タイムアウト、中断は `ChewingSensorService` が持つ。`ChewingCalibrationFlow` は「利用者の準備ができるまで待たせる」表示だけを持ち、フェーズ順序を判断しない (`ADR-CC-001`、`ADR-CC-002`)。

## Changed Files

| File | 変更内容 | 対応 ID |
| --- | --- | --- |
| `Assets/YummyVerse/Scripts/Model/Struct/ChewingCalibrationPhase.cs` | 新規。測定フェーズ (`Noise`/`Chew`) の値オブジェクト | `FR-CC-004` |
| `Assets/YummyVerse/Scripts/Model/Interface/IChewingCalibrationPrompt.cs` | 新規。フェーズ要求送信の直前に呼ばれる role-specific port | `FR-CC-004` |
| `Assets/YummyVerse/Scripts/Model/ChewingSensorProtocol.cs` | `CAL_NOISE`/`CAL_CHEW`/`CAL_ABORT` の構築、`CAL_NOISE_DONE`/`CAL_CHEW_DONE` の解釈を追加 | `FR-CC-001` |
| `Assets/YummyVerse/Scripts/Model/Struct/ChewingSensorMessage.cs` | `CalibrationNoiseDone`/`CalibrationChewDone` の種別と生成を追加 | `FR-CC-001` |
| `Assets/YummyVerse/Scripts/Model/Interface/IChewingSensorService.cs` | `CalibrateAsync(Action onAccepted, ...)` を `CalibrateAsync(IChewingCalibrationPrompt, ...)` へ変更 | `FR-CC-004` |
| `Assets/YummyVerse/Scripts/Model/ChewingSensorService.cs` | 保留要求を `CalibrationStage` (受理待ち/案内中/ノイズ測定中/咀嚼測定中) の状態機械へ作り替え。案内中は無期限に待ち、測定中だけ期限を切る。中断・タイムアウト時に `CAL_ABORT` を送る | `FR-CC-002`、`FR-CC-006`、`NFR-CC-002` |
| `Assets/YummyVerse/Scripts/Model/Struct/SO/ChewingSensorConfig.cs` | `calibrationCompletionTimeoutSeconds` を `calibrationNoiseTimeoutSeconds`/`calibrationChewTimeoutSeconds` へ分割 | `NFR-CC-002` |
| `Assets/YummyVerse/Scripts/ViewModel/Interface/IMessagePresenter.cs`、`MessagePresenter.cs` | 本文の下に差し替え可能な補助行 (`ShowAsync(msg, subText, ct)`、`SetSubText`) を追加。カウントダウンは本文を保ったまま下の行だけを更新する | `FR-CC-003` |
| `Assets/YummyVerse/Scripts/ViewModel/Tutorial/SO/TutorialConfig.cs` | `chewingCalibrationNoiseMessage`/`chewingCalibrationMeasuringMessage`/`chewingCalibrationCountdownSeconds` を追加し、旧 `chewingCalibrationHoldMessage`/`chewingCalibrationChewPromptDelaySeconds` を置換 | `FR-CC-007` |
| `Assets/YummyVerse/Scripts/ViewModel/Tutorial/ChewingCalibrationFlow.cs` | 案内 + カウントダウン + 「計測中...」を出す `CountdownPrompt` を実装。カウント 0 で待ちが明け、その時点で Model 側がフェーズ要求を送る | `FR-CC-003`、`FR-CC-005` |
| `Assets/YummyVerse/Editor/TutorialAssetBuilder.cs` | 新しい文言キーと既定値の生成を追加 | `FR-CC-007` |
| `Assets/YummyVerse/Scripts/Model/ChewingSensorService.cs` (追補) | キャリブレーション系の受信メッセージを `受信: ...` としてログへ出し、失敗・タイムアウトの警告にフェーズ名を含めた。送信だけがログに出る状態では「Unity が送っていない」のか「咀嚼計が返さない」のかを切り分けられなかったため | 実機観測 (`verification/test-results.md`) を受けた追補 |
| `Assets/YummyVerse/Editor/Tests/ChewingSensorProtocolTests.cs` | フェーズ命令の構築、フェーズ完了応答の解釈、`NOT_STARTED`/`ABORTED` 理由、形式不正の破棄をテストに追加 | `NFR-CC-004` |
| `Assets/YummyVerse/Editor/Tests/NarrationStepTests.cs` | `IMessagePresenter` の test double を新しい契約へ追従 | `NFR-CC-004` |

## Changed Assets

serialized asset は Editor メニューの再実行を待たずに動くよう、テキスト編集で同じ内容へ更新した。GUID と参照構造は変更していない。

| Asset | 変更内容 |
| --- | --- |
| `Data/Tutorial/Localization/TutorialStrings Shared Data.asset` | key `S1.ChewingCalibrationHold` を `S1.ChewingCalibrationNoise` へ改名 (ID `5229240322` は保持)。`S1.ChewingCalibrationMeasuring` (ID `5229240324`) を追加 |
| `Data/Tutorial/Localization/TutorialStrings_ja-JP.asset` | `5229240322` を「小さく歯をカチカチしてください」、`5229240323` を「奥歯でちゃんと噛みしめてください」へ更新。`5229240324` に「計測中...」を追加 |
| `Data/Tutorial/TutorialConfig.asset` | `chewingCalibrationNoiseMessage`/`chewingCalibrationMeasuringMessage`/`chewingCalibrationCountdownSeconds: 5` へ更新 |
| `Data/ChewingSensor/ChewingSensorConfig.asset` | `calibrationNoiseTimeoutSeconds: 30`/`calibrationChewTimeoutSeconds: 30` へ分割 |

`TutorialAssetBuilder` の `Str` は既存の非空エントリを上書きしないため、文言変更は asset 側の編集で行った。新規環境で Editor メニュー `YummyVerse > Tutorial > Create Default Tutorial Assets` を実行した場合も同じ key/既定文言が生成される。

## Deviations and Notes

- `CAL_CHEW_DONE` は終端として扱わず、待ち時間を取り直して `CAL_DONE` を待つ。閾値の確定・保存は `CAL_DONE` の直前に行われる仕様のため (仕様書 §9.1)。
- 案内中 (`Prompting`) は Unity 側でも期限を設けない。咀嚼計がフェーズ指示を無期限に待つと決まっているため (仕様書 §9.7)。切断だけが案内中の打ち切り条件である。
- 中断 (`CancellationToken` のキャンセル) と測定のタイムアウトでは `CAL_ABORT` を送る。保留要求がない場合と接続世代が変わっている場合は送らない。
- カウントダウンは既存の1つのメッセージ表示に本文 + 補助行として出す。Scene/Prefab の serialized reference は変更していない。

## Follow-up

- `verification/test-results.md` の `NOT-RUN` を解消する。
- 咀嚼計ファームウェアの v1.1 対応後、実機でフェーズ分割シーケンスとカウントダウンの体感を確認し、カウントダウン秒数と各フェーズのタイムアウト既定値を調整する。
