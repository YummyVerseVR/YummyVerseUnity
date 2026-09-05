# Chewing Sensor Serial Protocol Knowledge

## Authority and Scope

この文書は、YummyVerse の咀嚼計シリアル通信プロトコルに関して intent をまたいで再利用する安定知識を `aidlc` 内へ保存する。プロトコルの規範的なソース (normative source) はリポジトリ直下の `YummyVerse_Serial_Protocol_v1.1.md` (文書ID `YV-SERIAL-001`、プロトコルバージョン `1`、文書バージョン `1.1`) であり、本文書はそれを丸ごと複製しない。ここでは、YummyVerseUnity 側で実装・回帰確認するために必要な要点と、実際のクラス構成に基づく実装境界だけを自己完結して記す。

キャリブレーションのフェーズ分割対応そのものの要件・設計判断は `spaces/default/intents/260904-chewing-calibration-phase-split/` に記録する。本文書はその前提知識であり、intent 側の決定を上書きしない。

## 1. プロトコル v1.1 の要点

### 1.1 役割とシリアル設定

- Unity は COM ポート探索、`HELLO`/`READY` ハンドシェイク、キャリブレーション開始、開閉イベントの消費を担当する。
- 咀嚼計はハンドシェイク応答、キャリブレーション、開閉イベントの発行を担当する。
- 通信設定は 115200 baud / 8N1 / フロー制御なし / LF (`0x0A`) 終端で固定。CRC は本バージョンでは付与しない。
- メッセージは `FIELD_0,FIELD_1,...,FIELD_N\n` で、命令名・列挙値は大文字・小文字を区別する。本文は LF を除き 63 バイト以下。

### 1.2 COM ポート探索とハンドシェイク

- Unity はポートを固定せず、アクセス可能な COM ポートを列挙して各ポートへ `HELLO,YUMMYVERSE,1` を送る疑似ブロードキャストを行う。
- `READY,YUMMYVERSE,1,CHEWING_SENSOR` を返したポートだけを適合デバイスとして採用する。VID/PID・製品名・前回の COM 番号は探索順の最適化にのみ使ってよく、恒久除外の根拠にしない。
- ハンドシェイク成立前に送ってよい命令は `HELLO` だけ。
- 複数の適合デバイスを検出した場合、プロトコル v1 では Unity がユーザーに使用ポートを選択させる規定になっている (§6.2)。

### 1.3 キャリブレーションのフェーズ分割 (v1.1 の中心変更)

v1.0 は `CAL_START` → `CAL_ACCEPTED` → (咀嚼計が案内なしに一括測定) → `CAL_DONE` という形で、利用者が何をすべきか分からないまま測定が始まる問題があった。v1.1 は測定を「ノイズ測定」「咀嚼測定」の2フェーズに分割し、各フェーズの開始を Unity が明示的に指示する形に変えた。

```text
Unity  -> 咀嚼計 : CAL_START,<requestId>
咀嚼計 -> Unity  : CAL_ACCEPTED,<requestId>       # まだ測定していない
                    # Unity: 「小さく歯をカチカチさせてください」+ カウントダウン
Unity  -> 咀嚼計 : CAL_NOISE,<requestId>          # カウント0の時点で送る
咀嚼計 -> Unity  : CAL_NOISE_DONE,<requestId>
                    # Unity: 「奥歯でしっかり噛みしめてください」+ カウントダウン
Unity  -> 咀嚼計 : CAL_CHEW,<requestId>           # カウント0の時点で送る
咀嚼計 -> Unity  : CAL_CHEW_DONE,<requestId>
咀嚼計 -> Unity  : CAL_DONE,<requestId>           # 閾値確定・保存
```

- フェーズは `CAL_NOISE` → `CAL_CHEW` の順を厳守する。`CAL_START` 未受理、またはノイズ未完了で `CAL_CHEW` を要求すると `CAL_FAILED,<requestId>,NOT_STARTED` が返る (エラー理由 `NOT_STARTED` は v1.1 で新設)。
- 完了済みフェーズを再要求された場合、咀嚼計は再測定せず該当フェーズの完了応答を再送する。
- **仕様書 §9.2 は「各フェーズ要求の前にカウントダウンを表示し、カウントが 0 になった時点でフェーズ要求を送る」ことを Unity 側の実装必須要件と明記している。** これは v1.1 でフェーズ分割を導入した目的そのものであり、任意の UX 演出ではない。
- 咀嚼計はフェーズ指示を無期限に待つ (§9.7)。利用者の準備時間に上限を設けないのはデバイス側の仕様であり、Unity 側が持つタイムアウトは「Unity が要求を諦めて UI に失敗を返す」ためのものであって、デバイス側の待機時間とは独立している。
- `CAL_DONE` と `CAL_FAILED` は相互排他的な終端応答。
- キャリブレーション中、咀嚼計は `MOUTH` イベントを送信しない。

### 1.4 中断 (`CAL_ABORT`, v1.1 で新設)

```text
Unity  -> 咀嚼計 : CAL_ABORT,<requestId>
咀嚼計 -> Unity  : CAL_FAILED,<requestId>,ABORTED
```

利用者が操作を中止した場合、Unity は `CAL_ABORT,<requestId>` を送ってデバイス側のフェーズ状態を破棄させることができる。保留中の要求がない、または `requestId` が一致しない場合は無視される。Unity が要求を放棄する手段は「`CAL_ABORT` を送る」か「切断してリセットする」のいずれかであり (§9.7)、単に応答を待つのをやめるだけでは咀嚼計側の状態は残る。

### 1.5 requestId

- 32bit 符号なし整数、`0` は「保留要求なし」の内部予約値で電文には現れない。`uint.MaxValue` の次は `1` に折り返す。
- 一致判定だけを行い、大小比較で新旧を判断しない。
- Unity は新しい接続を確立した際、以前の接続の保留要求を失敗として終了させる (古い接続の応答を新しい要求に紐づけない)。

### 1.6 開閉イベント (`MOUTH`)

- `MOUTH,OPEN` / `MOUTH,CLOSED` は一方向のイベント型で、連番・ACK・再送・欠落回復を持たない。交互性も検証しない。
- 切断・再接続時は未処理イベントと部分受信行を破棄する。

### 1.7 エラー理由 (v1.1 時点)

`BUSY`、`NOT_STARTED` (v1.1 新設)、`ABORTED` (v1.1 新設のコマンドに対応)、`SENSOR_ERROR`、`SENSOR_UNSTABLE`、`INTERNAL_ERROR`。形式不正・未知命令・範囲外 requestId には `CAL_FAILED` を返さず行を破棄する。

### 1.8 切断・再接続

I/O 例外・ポート消失・明示的切断を検出したらポートを閉じ、部分受信行・未処理開閉イベント・保留中のキャリブレーション要求を破棄してから COM ポート探索とハンドシェイクをやり直す。COM ポート番号は再接続後に変わり得るため、固定しない。

## 2. Unity 側の実装境界 (現行コードの責務)

以下は 2026-09-04 時点で実際にリポジトリへ存在するコードの責務である。同日、`260904-chewing-calibration-phase-split` intent の Construction で v1.1 のフェーズ分割へ移行した (compile/実機検証は `NOT-RUN`)。クラス名・ファイルパスは実装境界の記録であり、変更時はこの表を更新すること。

| クラス / ファイル | 層 | 責務 |
|---|---|---|
| `ChewingSensorProtocol.cs` (`Model`) | Model | メッセージ文字列の構築・解釈だけを行う純粋関数群。ポート・スレッド・状態を持たず EditMode テストで直接検証できる。`HELLO`/`READY`/`CAL_START`/`CAL_NOISE`/`CAL_CHEW`/`CAL_ABORT` の構築と、`READY`/`CAL_ACCEPTED`/`CAL_NOISE_DONE`/`CAL_CHEW_DONE`/`CAL_DONE`/`CAL_FAILED`/`MOUTH` の解釈に対応。 |
| `Model/Struct/ChewingSensorMessage.cs` | Model | 受信1行を解釈した結果を表す不変 struct。`ChewingSensorMessageKind` は `Ready`/`CalibrationAccepted`/`CalibrationNoiseDone`/`CalibrationChewDone`/`CalibrationDone`/`CalibrationFailed`/`Mouth`。 |
| `Model/Struct/SO/ChewingSensorConfig.cs` | Model (SO) | ボーレート・読み取りタイムアウト・探索間隔・`HELLO` 再送間隔・ポート探索優先語・キャリブレーション関連タイムアウト (`CalibrationAcceptedTimeoutSeconds`、`CalibrationStartAttempts`、`CalibrationNoiseTimeoutSeconds`、`CalibrationChewTimeoutSeconds`)・咀嚼音フォールバックの現場調整値。 |
| `Model/Interface/ISerialPortProvider.cs` / `ISerialPortConnection.cs` | Model (port) | COM ポートの列挙・オープン・読み書きを抽象化する境界。テストではダミー実装に差し替え可能。 |
| `SerialLineAssembler.cs` (`Model`) | Model | バイトストリームを LF 区切りの1行へ組み直す。63 バイト超の行は次の LF まで破棄して同期回復する。 |
| `ChewingRequestIdSequence.cs` (`Model`) | Model | requestId の発番。`0` を予約し、`uint.MaxValue` からの折り返しを行う。 |
| `ChewingSensorService.cs` (`Model`) | Model | 常駐接続本体。受信専用スレッドで探索・ハンドシェイク・送受信・切断検知を行い、メインスレッド (`Tick`) で受信キューの消費、保留中キャリブレーション1件の管理、再送・タイムアウト判定、R3 (`ConnectionState`, `OnMouthEvent`) への発行を行う。`CalibrateAsync(prompt, ct)` は保留要求を `CalibrationStage` (`AwaitingAccept`/`Prompting`/`MeasuringNoise`/`MeasuringChew`) の状態機械として進める。案内中 (`Prompting`) は無期限に待ち、測定中だけフェーズ単位の期限を切る。中断・タイムアウト時は `CAL_ABORT` を送る。 |
| `Model/Interface/IChewingSensorService.cs` | Model (port) | ViewModel から見た口。接続状態・開閉イベント・`CalibrateAsync` のみを公開する。 |
| `Model/Struct/ChewingCalibrationResult.cs` | Model | キャリブレーション結果 (`Succeeded`/`Failed`/`TimedOut`/`NotConnected`)。例外を使わず結果型で表現する。 |
| `Model/Interface/IChewingCalibrationPrompt.cs` | Model (port) | フェーズ要求送信の直前に呼ばれる role-specific port。`PrepareAsync(phase, ct)` が完了するまで通信側は要求を送らない。 |
| `Model/Struct/ChewingCalibrationPhase.cs` | Model | 測定フェーズ (`Noise`/`Chew`) の値オブジェクト。 |
| `ViewModel/Tutorial/ChewingCalibrationFlow.cs` | ViewModel/Presentation | 「スタート操作の直後、S2 (チュートリアル本体開始) の手前」に挟まる案内。`IChewingCalibrationPrompt` を実装する `CountdownPrompt` が、フェーズごとに `TutorialConfig` の案内文言 + カウントダウンを出し、0 になった時点で「計測中...」へ差し替えて待ちを明ける。咀嚼計が未接続・失敗・タイムアウトでも例外にせず、案内を閉じてチュートリアルを続行する。 |
| `ViewModel/Interface/IChewingCalibrationFlow.cs` | ViewModel (port) | Tutorial 側から見た口。`RunAsync(ctx, ct)` のみ。 |
| `ViewModel/Tutorial/SO/TutorialConfig.cs` | ViewModel (SO) | `ChewingCalibrationNoiseMessage`、`ChewingCalibrationChewMessage`、`ChewingCalibrationMeasuringMessage` (いずれも `LocalizedString`)、`ChewingCalibrationCountdownSeconds` を現場調整値として保持する。 |
| `ViewModel/Interface/IMessagePresenter.cs` | ViewModel (port) | 本文に加えて差し替え可能な補助行 (`ShowAsync(msg, subText, ct)`、`SetSubText`) を持つ。カウントダウンは本文を保ったまま下の行だけを更新する。 |
| `Editor/Tests/ChewingSensorProtocolTests.cs` | Editor test | `ChewingSensorProtocol` の文字列構築・解釈に対する EditMode 回帰。仕様書 §5〜§11、適合確認チェックリスト §18 の文字列解釈で決まる項目を固定する。 |

### 2.1 現行実装の設計上の要点 (v1.1 対応時も維持すべき前提)

- `ChewingSensorService` は受信スレッドとメインスレッドを明確に分離し、メインスレッドをシリアル I/O でブロックしない (仕様書 §15.1)。フェーズ分割後もこの分離は維持する。
- 保留できるキャリブレーション要求は同時に1件だけ (`_pending`)。2件目は `BUSY` として即座に失敗を返す。
- 接続の世代番号 (`_connectionEpoch`) により、古い接続への応答を新しい要求に紐づけない (仕様書 §10)。
- `ChewingCalibrationFlow` は例外を投げない失敗時継続方針を持つ。これは無人展示で1台の不調が来場者を足止めしないための意図的な設計であり、v1.1 対応後も変更しない。

## 3. v1.0 → v1.1 の差分と移行の記録

2026-09-04 に v1.1 のフェーズ分割へ移行した。詳細な要件・ADR は `260904-chewing-calibration-phase-split` intent、変更ファイルの一覧は同 intent の `construction/implementation.md` を正とする。ここでは以後の変更で踏まないための要点だけを残す。

1. **プロトコル層**: `CAL_NOISE`/`CAL_CHEW`/`CAL_ABORT` の構築と `CAL_NOISE_DONE`/`CAL_CHEW_DONE` の解釈を追加した。`CAL_FAILED` の理由フィールドは元から自由文字列なので、`NOT_STARTED`/`ABORTED` はパーサ変更なしで通る。回帰テストで明示的に固定してある。
2. **通信層**: 単一フェーズの待ち合わせを `CalibrationStage` の状態機械へ置き換えた。段ごとに待ち方が違うのが要点で、受理待ちは `CAL_START` を再送し、案内中は期限を設けず、測定中だけフェーズ単位の期限を切る。案内中に期限を設けないのは、咀嚼計がフェーズ指示を無期限に待つと決まっているため (仕様書 §9.7)。
3. **終端の位置**: `CAL_CHEW_DONE` は終端ではない。閾値の確定・保存は `CAL_DONE` の直前に行われるので、`CAL_CHEW_DONE` では待ち時間を取り直すだけにして `CAL_DONE` を待つ (仕様書 §9.1)。
4. **中断**: 中断とタイムアウトで `CAL_ABORT` を送る。送らずに待つのをやめるとデバイス側にフェーズ状態が残り、次の来場者の要求が `BUSY` で弾かれる。保留要求がない場合と接続世代が変わっている場合は送らない。
5. **表示との結合**: フェーズ順序の判断は通信層に閉じ、Presentation 層は `IChewingCalibrationPrompt` を通じて「準備ができた」とだけ答える。案内・カウントダウンを通信層へ、フェーズ順序を Presentation 層へ移さない。
6. **現場調整値**: フェーズ別のタイムアウト (`ChewingSensorConfig`)、カウントダウン秒数と3種類の案内文言 (`TutorialConfig` + `TutorialStrings`) を追加した。`TutorialAssetBuilder` の `Str` は既存の非空エントリを上書きしないため、**既存文言の変更は Editor メニュー再実行では反映されない**。文言を変えるときは Localization テーブル側を直接編集するか、新しいキーを使う。
7. **失敗時継続方針**: 未接続・各フェーズの失敗・タイムアウト・中断のいずれでも例外にせず結果型で返し、案内を閉じてチュートリアルを続行する。これは v1.0 から不変であり、無人展示で1台の不調が来場者を足止めしないための意図的な設計である。

## 4. 回帰確認観点

- ハンドシェイク: `HELLO` の重複送信が副作用を起こさないこと。
- キャリブレーション正常系: `CAL_START` → `CAL_ACCEPTED` → (案内+カウントダウン) → `CAL_NOISE` → `CAL_NOISE_DONE` → (案内+カウントダウン) → `CAL_CHEW` → `CAL_CHEW_DONE` → `CAL_DONE` の一連で、各フェーズ要求がカウント 0 の時点で送信されること。
- 順序: ノイズ未完了で `CAL_CHEW` 相当の状態に進まないこと。実機から `NOT_STARTED` を受けても例外にならず、失敗として処理されチュートリアルが続行すること。
- 中断: セッションリセット・利用者離脱・StaffReset でキャリブレーション中断時に `CAL_ABORT` が送られ、保留要求が破棄されること。`ABORTED` 理由の `CAL_FAILED` を受けても展示が止まらないこと。
- 失敗系: 各フェーズでの `CAL_FAILED` (`SENSOR_ERROR`/`SENSOR_UNSTABLE`/`BUSY`/`INTERNAL_ERROR`/`NOT_STARTED`/`ABORTED` いずれでも) が例外を発生させず、案内を閉じてチュートリアルへ進むこと。
- 切断・再接続: フェーズ途中の切断で保留要求が `NotConnected` として打ち切られ、再接続後は新規 `CAL_START` から開始できること。部分受信行が破棄されること。
- requestId: 折り返し・重複再送時の一致判定が既存挙動から変わらないこと。
- 現場調整: `TutorialConfig` のカウントダウン秒数・文言変更、および `TutorialStrings` への新規文言追加後の Editor メニュー再実行が実機表示へ反映されること。
- EditMode: 新規プロトコル文字列のパース/構築が `ChewingSensorProtocolTests` で検証されること。
- 実機: 各フェーズの実測時間は未確認。咀嚼計ファームウェアのフェーズ応答は未対応であることが 2026-09-04 に判明した (下記)。

## 5. 未解決事項

- 咀嚼計ファームウェアは 2026-09-04 時点でフェーズ応答 (`CAL_NOISE_DONE`/`CAL_CHEW_DONE`) を返さず、`CAL_NOISE` に対して v1.0 相当の `CAL_DONE` を返す。`CAL_ABORT`/`NOT_STARTED` の対応状況は未確認。詳細は `260904-chewing-calibration-phase-split/verification/test-results.md`。
- ノイズ測定・咀嚼測定それぞれの実測時間 (デバイス側の測定所要時間) は未確認。Unity 側のフェーズ完了待ちタイムアウト初期値は仕様書 §13 の推奨値を暫定採用する前提とする。
- カウントダウン秒数の現場最適値は未確定 (仕様書は「3秒程度を想定」とだけ記す)。
- 仕様書 §6.2 が要求する「複数適合デバイス検出時のユーザーポート選択」導線は、現行 `ChewingSensorService.Discover()` が最初に見つかった適合ポートを即採用する実装になっており、選択 UI の存在は本文書作成時点で確認できていない。これはフェーズ分割対応とは別の既知の gap として記録する。
- 実機接続でのフェーズ分割シーケンスの動作確認、Editor/Quest/PCVR 実行結果は本文書作成時点で `NOT-RUN`。
