# Intent Statement

## Problem

咀嚼計シリアル通信プロトコルの v1.0 は、`CAL_START` の受理 (`CAL_ACCEPTED`) から完了 (`CAL_DONE`) まで咀嚼計が一括で測定を行う形だった。利用者から見て「いつ何をすればよいか」が分からず、Unity が指示を出す前に測定が始まってしまう問題があった。プロトコル v1.1 (`YummyVerse_Serial_Protocol_v1.1.md`) はキャリブレーションを「ノイズ測定」「咀嚼測定」の2フェーズへ分割し、`CAL_NOISE`/`CAL_NOISE_DONE`/`CAL_CHEW`/`CAL_CHEW_DONE` を新設した。仕様書 §9.2 は「各フェーズ要求の前にカウントダウンを表示し、カウントが 0 になった時点でフェーズ要求を送る」ことを Unity 側の実装必須要件と明記している。中断用に `CAL_ABORT` も追加された。

現行の Unity 実装 (`ChewingSensorService.cs`、`ChewingCalibrationFlow.cs`) は v1.0 形状のままである。`ChewingCalibrationFlow` は `CAL_ACCEPTED` を受けたら固定 `ChewingCalibrationChewPromptDelaySeconds` 秒 (既定5秒) 後に案内文言を「もぐもぐしてください」へ差し替えるだけで、デバイスの実際の測定完了とは連動しないタイマー実装になっている。`ChewingSensorService.CalibrateAsync` も `CAL_START` → `CAL_ACCEPTED` → `CAL_DONE`/`CAL_FAILED` という単一フェーズの結果しか扱えず、`CAL_NOISE`/`CAL_CHEW`/`CAL_ABORT` の構築・解釈にも対応していない。

## Desired Outcome

- `ChewingSensorService` (Model 層) が `CAL_NOISE` → `CAL_NOISE_DONE` → `CAL_CHEW` → `CAL_CHEW_DONE` → `CAL_DONE` のフェーズ順序、再送、タイムアウトを保持する。
- `ChewingCalibrationFlow` (Presentation/ViewModel 層) が各フェーズの案内文言とカウントダウンを表示し、カウントが 0 になった時点でフェーズ要求を送信できるようにする。画面フローは次の通り。
  1. 「小さく歯をカチカチしてください」+ 5秒カウントダウン → 0 で「計測中...」に切り替わると同時に `CAL_NOISE` を送信する。
  2. `CAL_NOISE_DONE` 受信後、「奥歯でちゃんと噛みしめてください」+ 5秒カウントダウン → 0 で「計測中...」+ `CAL_CHEW` を送信する。
  3. `CAL_DONE` 受信でキャリブレーション案内を閉じ、既存のチュートリアル本体 (「YummyVerse へようこそ」= S2) へ遷移する。
- 上記2層を role-specific port `IChewingCalibrationPrompt` (各フェーズ要求送信の直前に呼ばれ、await が完了するまで送信を待たせる) で接続し、フェーズ順序の判断を ViewModel 側に漏らさない。
- 咀嚼計の未接続・失敗・タイムアウトでも例外にせず、案内を閉じてチュートリアルを続行する既存方針 (v1.0 から不変) を維持する。無人展示で1台の不調が来場者を足止めしないための方針である。
- 利用者が操作を放棄した場合、Unity は `CAL_ABORT` を送ってデバイス側のフェーズ状態を破棄させる。
- カウントダウン秒数と案内文言を `TutorialConfig` (ScriptableObject) の設定値・Localization テーブルで現場調整可能にする。

## Scope

- `Assets/YummyVerse/Scripts/Model/ChewingSensorProtocol.cs`: `CAL_NOISE`/`CAL_CHEW`/`CAL_ABORT` の構築、`CAL_NOISE_DONE`/`CAL_CHEW_DONE` の解釈への対応方針。
- `Assets/YummyVerse/Scripts/Model/ChewingSensorService.cs`: フェーズ順序・再送・タイムアウトの状態遷移設計。
- `Assets/YummyVerse/Scripts/Model/Struct/ChewingSensorMessage.cs`: 新規メッセージ種別の追加方針。
- `Assets/YummyVerse/Scripts/Model/Struct/SO/ChewingSensorConfig.cs`: フェーズ単位のタイムアウト現場調整値の追加方針。
- `Assets/YummyVerse/Scripts/ViewModel/Tutorial/ChewingCalibrationFlow.cs`: 案内表示・カウントダウンの実装方針と、新規 port `IChewingCalibrationPrompt` の実装方針。
- `TutorialConfig` と `TutorialStrings` Localization テーブルへの新規文言・カウントダウン秒数の追加方針 (実際の SO/テーブル編集と Editor メニュー `YummyVerse > Tutorial > Create Default Tutorial Assets` の再実行は実装作業者が行う)。
- `Assets/YummyVerse/Editor/Tests/ChewingSensorProtocolTests.cs` に追加すべき回帰観点の設計。

## Out of Scope

- `Assets/`、`README.md`、`YummyVerse_Serial_Protocol_v1.1.md`、`ProjectSettings/` の実際のコード・アセット編集 (本 intent は documentation-only)。
- 咀嚼計ファームウェア側の実装・検証。
- 仕様書 §6.2 が要求する「複数適合デバイス検出時のユーザーポート選択」導線の実装状況調査・改修 (`chewing-sensor-serial-protocol.md` に既知の gap として記録済みだが、本 intent の変更対象ではない)。
- Tutorial 本体 (S2 以降のステップ列) のシーケンス変更。

## Stakeholders

- 実装作業者: 本 intent の要件・ADR に従って `Assets/` のコードと Editor 資産を変更する。
- 展示運営者: キャリブレーション案内文言・カウントダウン秒数を現場で調整する。
- 来場者: フェーズごとの指示に従ってキャリブレーションを行う。
- 咀嚼計ファームウェア開発者: v1.1 のフェーズ分割コマンドと `CAL_ABORT` に応答する。

## Success Criteria

1. フェーズ順序・再送・タイムアウトの責務が Model 層 (`ChewingSensorService`) に、案内表示・カウントダウンの責務が Presentation 層 (`ChewingCalibrationFlow`) にあることが要件・ADR として明記されている。
2. 両者を接続する role-specific port `IChewingCalibrationPrompt` の契約 (各フェーズ要求送信の直前に呼ばれ、await 完了まで送信を待たせる) が記録されている。
3. 各フェーズ要求がカウントダウン 0 の時点で送信される、という仕様書 §9.2 の実装必須要件を満たす設計になっている。
4. `CAL_ABORT` を送る条件 (利用者の中断・セッションリセット・離脱) が明記されている。
5. 咀嚼計の不調時にも Tutorial を止めない既存方針が、フェーズ分割後の設計でも明示的に維持されている。
6. `TutorialConfig`/Localization での現場調整と、それに伴う Editor メニュー再実行の必要性が記録されている。
7. Construction/Verification が未実施であることが `NOT-RUN` として明示され、成功として記録されていない。
