# Chewing Calibration Phase Split Test Results

## Environment

- Date: 2026-09-04
- Unity Editor: `6000.2.6f2`（作業時点で既存 Editor instance が project を開いており、batchmode instance は起動できない）
- Out-of-editor compile: `dotnet` は当該環境に未インストールのため実行できない
- Scope: プロトコル v1.1 フェーズ分割対応のコード変更と asset 変更 (`construction/implementation.md`)

## Gate Results

| Gate | 内容 | Result | Evidence |
| --- | --- | --- | --- |
| `G-CC-01` | C# compile | `PASS` | 検証者の Editor が変更後のコードをコンパイルし Play Mode で実行した。`Editor.log` のスタックに `ChewingSensorService.RunPhaseAsync`／`RunCalibrationAsync`／`CalibrateAsync` が新しい行番号で現れており、compile error なしに新実装が動作したことを示す |
| `G-CC-02` | EditMode テスト (`ChewingSensorProtocolTests`、`NarrationStepTests`) | `NOT-RUN` | テストケースは追加済みだが Test Runner を実行していない |
| `G-CC-03` | Unity Editor での asset load / 参照検証 | `NOT-RUN` | 静的なテキスト検証のみ実施（下記）。Editor による実 load、Inspector 表示、DI 解決は未確認 |
| `G-CC-04` | 咀嚼計実機とのフェーズ分割シーケンス確認 | `NOT-RUN` | ファームウェアの v1.1 対応状況が未確認 |
| `G-CC-05` | PCVR での画面フロー確認（案内・カウントダウン・計測中表示・S2 への遷移） | `NOT-RUN` | 未実行。`G-CC-03` から推測しない |

## Static Verification

`G-CC-03` の代替ではなく、その前提として実施した静的検査。

| Check | Result | Evidence |
| --- | --- | --- |
| 削除した API の残存参照 | `PASS` | `CalibrationCompletionTimeoutSeconds`、`MarkAccepted`、`IsAccepted`、`AcceptedAt`、`ChewingCalibrationHoldMessage`、`ChewingCalibrationChewPromptDelaySeconds` の参照は `Assets` 配下に 0 件 |
| `IMessagePresenter` 実装の追従 | `PASS` | 実装は `MessagePresenter` と test double の `RecordingMessagePresenter` の 2 件で、いずれも新しい契約へ更新済み |
| 新規 script の `.meta` GUID | `PASS` | `ChewingCalibrationPhase.cs.meta`、`IChewingCalibrationPrompt.cs.meta` を追加。`Assets` 配下の既存 GUID と重複なし |
| Localization key と `TutorialConfig` 参照の整合 | `PASS` | `S1.ChewingCalibrationNoise`/`S1.ChewingCalibrationChew`/`S1.ChewingCalibrationMeasuring` が shared data・ja-JP table・`TutorialConfig.asset` の 3 箇所で一致 |

## Observed Run (2026-09-04, Editor Play Mode / 咀嚼計は未装着)

`C:\Users\dennou\AppData\Local\Unity\Editor\Editor.log` から確認した実際の送受信。**この実行では咀嚼計を誰も装着していない**ため、測定結果に依存する応答 (`SENSOR_UNSTABLE` 等) は device の欠陥の証拠にならない。`G-CC-04` の合格判定ではなく、Unity 側の送信シーケンスが動いたことの観測記録として扱う。

| # | 観測 | 判定 |
| --- | --- | --- |
| 1 | `咀嚼計を COM12 で検出しました` → `送信: CAL_START,1` → (`CAL_ACCEPTED,1` 受信) → `送信: CAL_NOISE,1` | Unity 側のフェーズ要求送信は仕様どおり動作している |
| 2 | `CAL_NOISE,1` の後、咀嚼計が `CAL_FAILED,1,SENSOR_UNSTABLE` を返す | 未装着なので測定値が安定しないのは当然であり、期待どおりの応答。Unity は `FR-CC-005` どおり案内を閉じてチュートリアルを続行した |
| 3 | 2 の結果、咀嚼測定フェーズへ進まず「奥歯でちゃんと噛みしめてください」は表示されない | 仕様上の正しい振る舞い。`CAL_FAILED` は終端応答であり、同一要求でフェーズを続行できない (仕様書 §9.4) |
| 4 | 次のサイクルで `送信: CAL_START,2` → `送信: CAL_NOISE,2`。以降、セッション終了まで `CAL_NOISE_DONE,2` も `CAL_FAILED,2` も返らなかった | 未装着かつセッションが先に終了した実行なので、device の無応答と断定しない。装着状態での再実行で確認する |
| 5 | `解釈できない受信行を破棄しました` と `保留中の要求と対応しない応答を破棄します` はログに 0 件 | 受信行の取りこぼし・解釈失敗は起きていない |

## Observed Run (2026-09-04, Editor Play Mode / 咀嚼計を装着)

受信ログを追加した後、咀嚼計を装着した状態で通した実行。

```text
[ChewingSensor] 送信: CAL_START,1
[ChewingSensor] 受信: CAL_ACCEPTED,1
[ChewingSensor] 送信: CAL_NOISE,1
[ChewingSensor] 受信: CAL_DONE,1
[Tutorial] Enter S2
```

- `解釈できない受信行を破棄しました` / `保留中の要求と対応しない応答を破棄します` は 0 件。受信の取りこぼしではない。
- 咀嚼計は `CAL_NOISE` に対して `CAL_NOISE_DONE` を返さず、終端応答である `CAL_DONE` を返した。
- Unity は仕様書 §9.1 のとおり `CAL_DONE` を終端として扱い、キャリブレーションを完了させて S2 へ進んだ。結果として咀嚼測定フェーズの案内は表示されない。

## Gap: 咀嚼計ファームウェアがフェーズ応答を返さない

- 確認日: 2026-09-04（装着状態、`Editor.log` による）。
- 咀嚼計は `CAL_START` → `CAL_ACCEPTED` と `CAL_NOISE` の受理までは動作するが、`CAL_NOISE_DONE`・`CAL_CHEW_DONE` を返さず、v1.0 相当の一括完了 (`CAL_DONE`) で応答している。`CAL_CHEW` には一度も到達していない。
- これは device 側の未対応であり、Unity 側で回避しない。`CAL_DONE` を受けた後に咀嚼測定の案内を出しても、咀嚼計は既に測定を終えているため案内が実測と対応しなくなる (仕様書 §9.2 の目的に反する)。
- 誤って Unity 側の不具合と解釈されないよう、咀嚼測定フェーズに到達する前に `CAL_DONE` を受信した場合は警告をログへ出す実装を追加した (`construction/implementation.md`)。
- ファームウェアが `CAL_NOISE_DONE`/`CAL_CHEW_DONE` を返すようになるまで、`G-CC-04`/`G-CC-05` は合格にできない。

## Still Unverified

- **咀嚼計を装着した状態での通し確認**が未実施。`CAL_NOISE_DONE` → 咀嚼測定の案内 → `CAL_CHEW` → `CAL_CHEW_DONE` → `CAL_DONE` の後半は一度も到達していない。ここが確認できるまで `G-CC-04`/`G-CC-05` は `NOT-RUN` のままとする。
- 上記の未装着実行を「ファームウェアの欠陥」と解釈しないこと。測定結果に依存する応答は、装着状態でしか判定できない。
- この実行を受けて、キャリブレーション系の受信メッセージ (`受信: ...`) と失敗・タイムアウト時のフェーズ名をログへ出すよう実装を追加した (`construction/implementation.md`)。送信だけがログに出る状態では、無応答・解釈失敗・device 側の拒否を切り分けられなかったため。

## Regression Points (未確認、実行時に確認する)

- 案内表示中に切断された場合、フェーズ要求を送らずに案内を閉じてチュートリアルへ進むこと。
- 咀嚼計が `CAL_FAILED`（`NOT_STARTED`/`SENSOR_UNSTABLE` 等）を返しても、セッションが中断せず S2 へ進むこと。
- セッション中断 (`UserAbsent`/`StaffReset`) で `CAL_ABORT` が送られ、次の来場者の要求が `BUSY` で弾かれないこと。
- カウントダウン中と「計測中...」表示中に本文が入れ替わらないこと（本文 + 補助行の組み立てが壊れていないこと）。
- キャリブレーション中に `MOUTH` イベントで咀嚼音が鳴らないこと（デバイス側の責務だが、Unity 側で二重に鳴らないことも見る）。
- 咀嚼計未接続時に `ConnectionWaitSeconds` 経過後、案内を出さずにチュートリアルへ進む従来動作が変わっていないこと。
