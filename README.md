# 概要
食感再現VRアプリケーション「YummyVerse」のUnity側のリポジトリです。

# インストール方法

## 1. Unity HubとUnity Editorのインストール
Unity Hubをインストールし、Unity Editorのバージョン6000.2.6f2をインストールします。

## 2. リポジトリのクローン
このリポジトリをクローンします。
安定して動くバージョンは最新のmainブランチにあります。

## 3. プロジェクトを開いてビルドする
Ctrl + Shift + B (MacOSの場合はCmd + Shift + B)でビルドウィンドウを開き、PlatformsをMeta Questに設定してBuildを押します。

ビルドに必要なシーンは `Restaurant` シーンのみです。

## 4. Quest 3で実験的機能を有効にする

> [!WARNING] 
> <font color="Red">この作業はQuest 3を再起動するたびに行なってください</font>


Meta Quest Developer Hubをインストールし、Quest 3をPCに接続します。
次に、左側のメニューから `Device Manager` を選択し、画面下部の `Custom Command` セクションの `Create Command` ボタンを押します。

![Custom Command](./docs_image/adb.png)

`Name` には適当な名称を入力します。 `COMMAND` には、以下のコマンドを入力します。

``` 
adb shell setprop debug.oculus.experimentalEnabled 1
```

`Save` ボタンを押してコマンドを保存し、作成したコマンドの `Run` ボタンを押します。

## 5. アプリケーションのインストール
 `Add Build` ボタンを押して、ビルドしたapkファイルを選択します。
インストールが完了したら、アプリ名 `com.DefaultCompany.YummyTemplate` の左側にチェックマークが表示されます。

# 使い方
## 食べる
初回は運営向け管理画面で Spatial Anchor と食べ物の表示位置を設定します。
設定後、QRコードを見つめると、そのGUIDに対応する食べ物が保存済みの位置へ表示されます。
QRコードは食べ物の選択にだけ使用し、表示位置と回転には使用しません。

## 咀嚼計(シリアル接続)
開口・閉口を検知するハードウェア「咀嚼計」を USB シリアルで接続すると、噛むたびに咀嚼音が鳴ります。
通信仕様は [`YummyVerse_Serial_Protocol_v1.0.md`](./YummyVerse_Serial_Protocol_v1.0.md) (115200 8N1 / LF終端) に準拠します。

- COMポート番号の設定は不要です。アプリはアクセスできるポートへ順に `HELLO,YUMMYVERSE,1` を送り、
  `READY,YUMMYVERSE,1,CHEWING_SENSOR` を返したポートを咀嚼計として採用します。
- 抜き差ししても自動で再探索します。咀嚼計が無くても他の機能はそのまま動作します。
- スタートボタンを押した直後に、来場者ごとのキャリブレーションが入ります。
  「口を動かさないでください」→ (受理から5秒後)「もぐもぐしてください」→ 完了で「YummyVerse へようこそ」へ進みます。
  咀嚼計が繋がっていない・失敗した場合はキャリブレーションを飛ばして先へ進みます。
- 咀嚼音は `MOUTH,OPEN` / `MOUTH,CLOSED` のどちらでも鳴ります。再生中に次の信号を受けたら頭から鳴らし直します。

> [!NOTE]
> シリアル通信は Windows / macOS / Linux のみ対応です。Quest 単体実行(Android)では咀嚼計なしとして動作します。

### 咀嚼音のデータ
咀嚼音は食品ごとに切り替わります。取得元は食品の種類で決まります。

| 食品の種類 | 咀嚼音の取得元 |
|---|---|
| `PersistentDataPath/Foods/<食品名>/` のローカル食品 | 同じフォルダ内の `audio.mp3` / `audio.wav` / `audio.ogg` (この優先順で最初に見つかったもの) |
| API v2 から取得した食品 | generated order の status にある `wav.artifact_id` に対応する音声 artifact |
| StandaloneMode の built-in food (`TestData/*.glb`) | 音声を置く場所の規約が無いため、既定の咀嚼音 |

音声が用意されていない、あるいは取得・デコードに失敗した食品では、
`ChewingSensorConfig` の `Fallback Chew Sound` を鳴らします。
どちらも無い場合は無音になります(食品の表示自体は続きます)。

> [!NOTE]
> generated order の咀嚼音は `AUDIO_GENERATION` が作る WAV artifact です。正式な取得経路は
> `GET /v2/devices/unity/orders/{order_id}/artifacts/{artifact_id}/download` (`audio/wav`) で、
> order status の `wav.downloadable` が true のときだけ `wav.artifact_id` が返ります。
> URL 生成 (`YummyServiceV2Url.TryBuildUnityDeviceArtifactDownloadUrl`) と
> 受け入れ判定 (`YummyServiceV2ContractGuard.TryAcceptSelectedWav` / `TryAcceptDownloadedWav`) は
> 実装済みで、Unity adapter はこの Device API 経路だけを使います。`/v2/menu` の公開 sample は
> generated order の代替として読み込みません。

### YummyService v2 API
API contract の snapshot と Unity 側で利用する schema は [`aidlc/spaces/default/knowledge/aidlc-shared/yummy-service-v2-unity-api.md`](./aidlc/spaces/default/knowledge/aidlc-shared/yummy-service-v2-unity-api.md) に記録しています。

- generated food の履歴は `/v2/devices/unity/orders`、選択した GLB/WAV は order の `artifact_id` を使う Device API を利用します。
- `/v2/menu` は公開された開発用 sample menu で、generated order の履歴・artifact revision・preview の代用ではありません。
- Unity adapter は `UNITY` Device token を `Authorization: Bearer ...` で送信します。token は build や設定 asset に埋め込まず、設定画面の `YummyService v2 Device Token` 欄へ実行時に入力してください。

#### ローカル Mock での接続確認

正式な production URL はまだ契約で公開されていません。手元で確認できる
`YummyService/YummyApiMock` を起動する場合は、Mock のディレクトリで次を実行します。

```sh
uv sync
uv run python -m src.entry
```

Mock は `http://127.0.0.1:8010` で待ち受けます。アプリの設定画面で次を入力してから
`Test Connection` を押してください。

| 項目 | ローカル Mock の値 |
|---|---|
| `YummyService Endpoint` | `http://127.0.0.1:8010` |
| `YummyService v2 Device Token` | `v2-unity-device-token` |

この token は Mock 専用の公開テスト資格情報です。production では Admin の device lifecycle
で発行した `device_type=UNITY` token を使用してください。`127.0.0.1` は Mock とアプリを同じ
マシンで動かす場合だけ有効で、Quest から別マシンの Mock へ接続する場合は到達可能な HTTPS
endpoint を設定します。

### 設定
`Assets/YummyVerse/Data/ChewingSensor/ChewingSensorConfig.asset` で調整します。

- `Fallback Chew Sound` … 食品ごとの咀嚼音が無いときに鳴らす音。
- `Chew Sound Volume` … 咀嚼音の音量。
- `Port Probe Timeout Seconds` / `Hello Retry Interval Seconds` … ポート探索の粘り強さ。
- `Calibration Completion Timeout Seconds` … `CAL_DONE` を待つ上限。センサー側の最大処理時間に合わせてください。

案内の文言と「もぐもぐしてください」への切り替え秒数は
`Assets/YummyVerse/Data/Tutorial/TutorialConfig.asset` にあります。

## 食べ物を破壊する
右手コントローラーの `B` ボタンを押すことで、現在シーンに存在する食べ物を破壊することができます。

## 【運営向け】管理画面
　2026年2月の更新で管理画面がつきました。管理画面はコントローラーの `A` ボタンを押すことで表示できます。

![Config UI](./docs_image/configui.png)

- `YummyService Endpoint` には、YummyService の server root または `/v2` root の URLを入力します。`/` の有無は問いません。`YummyService v2 Device Token` には運営から発行された `device_type=UNITY` token を入力してください。`Test Connection` は認証付きで `GET /v2/devices/unity/orders?state=COMPLETED&limit=1` を送り、10秒以内に結果を表示します。

- `Food Scale`は食べ物の大きさを調整できます。食べ物が大きすぎる、小さすぎる場合に利用してください。

- `Set / Update Spatial Anchor` は、水色の設定用Cubeの現在位置へ展示基準となるSpatial Anchorを作成して保存します。周囲が十分に見える明るい場所で実行してください。

- `Lock Food Position` は、コントローラーのGripで移動した設定用Cubeの位置を食べ物の表示位置として固定します。先にSpatial Anchorを設定する必要があります。Anchor UUIDとAnchorからの相対位置は端末へ保存され、次回起動時に復元されます。

- `QR Detection & Food 3D Model Download Status` では、Quest 3が認識したQRコードに書かれているGUIDと、直近の食べ物の3Dモデルのダウンロードリクエストの成否(表示内容はHTTP Status Code準拠)が表示されます。後述する `StandaloneMode` 時には、ファイル読み込み結果が表示されます。

- `Standalone Mode` を有効にすると、Yummy Control Serverに依存せずに事前に用意した食べ物を表示することができます。また、画面右側に `Standalone Foods` ウィンドウが表示されます。

  - `Standalone Foods` ウィンドウでは食べ物の名前が書かれたボタンを押すことで、食べ物を表示することができます。

> [!NOTE]
> `Standalone Mode` でも通常モードと同じ保存済みSpatial Anchor位置を使用します。表示位置を決めるためのQRコードは不要です。

> [!WARNING]
> StandaloneModeでは、Quest 3上の `storage/emulated/0/Android/data/com.DefaultCompany.YummyTemplate/TestData` 内から以下の3つのファイルを参照しています。(余談ですが、このパスがUnityにおける `Application.PersistentDataPath` 内の`TestData` フォルダです。)
> - `curry.glb`
> - `shrimp.glb`
> - `hamburg.glb`
> - `dragonsteak.glb` (2026/3/2 の更新で新規対応しました)
>
> **これらのファイルが配置されていない場合、StandaloneModeは動作しません！！！！**
>
> 2026/2/21 時点で、ファイルの転送は `adb` コマンドを使った方法が利用可能であることを確認しています。

# トラブルシューティング
## 確認項目( `StandaloneMode` が無効)
1. Quest 3はインターネットに接続されている？
  - インターネットに接続されていない場合は接続してください。

2. エンドポイントは正しく設定されている？
  - 有効なエンドポイントを設定してください。

3. `Test Connection` 結果は `Reached Host` かつ `Status : OK` になっている？
  - `Not Reached Host`の場合、サーバーまでのネットワークに何らかの問題があります。`Reached Host` でありながら `Status : OK` でない場合、エンドポイントのURLが誤っているか、サーバーに何らかの不具合が生じています。 (ここの原因の切り分けは、詳しい人に `Status` の内容を見せながら臨機応変に対応してください。)

4. `Last Detected GUID` が読み込んだQRコードの値に更新されているか？
  - `Last Detected GUID` が更新されない場合には、Quest 3の実験的機能が有効化されていない可能性があります。 `4. Quest 3で実験的機能を有効にする` の章を参考に、ADBコマンドで実験的機能を有効化してください。

5. 管理画面のSpatial Anchor状態が `Food position is fixed` または復元完了になっているか？
  - 未設定の場合は、水色のCubeを希望位置へ置いて `Set / Update Spatial Anchor` を押し、必要に応じてCubeを移動してから `Lock Food Position` を押してください。
  - 保存に失敗する場合は周囲を見回して空間情報を増やし、照明を確認して再実行してください。

## 確認項目( `StandaloneMode` が有効)
1. `.glb` ファイルが所定のディレクトリに配置されているか？
  - `【運営向け】管理画面` の後半のStandaloneModeの説明を読んでください。

---
最終更新 : 2026/8/30

更新者 : Inoyu
