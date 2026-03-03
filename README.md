# 概要
食感再現VRアプリケーション「YummyVerse」のUnity側のリポジトリです。

# インストール方法

## 1. Unity HubとUnity Editorのインストール
Unity Hubをインストールし、Unity Editorのバージョン6000.2.0f1をインストールします

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
開いたシーンでQRコードを見つめていれば食べ物が出てきます。
2026年2月の更新で、シーンを再読み込みしなくても読み込むQRを変えることで他の食べ物を体験できるようになりました。

## 【運営向け】管理画面
　2026年2月の更新で管理画面がつきました。管理画面はコントローラーの `A` ボタンを押すことで表示できます。

![Config UI](./docs_image/configui.png)

- `YummyControlServer Endpoint` には、Yummy Control ServerのエンドポイントのURLを入力します。URLの末尾に `/` を入れるのを忘れないようにしてください。また、` Test Connection` ボタンを押すと、エンドポイントの直下に `GET` リクエストを送信し、10秒以内にアクセス結果が表示されます。

- `Food Scale`は食べ物の大きさを調整できます。食べ物が大きすぎる、小さすぎる場合に利用してください。

- `QR Detection & Food 3D Model Download Status` では、Quest 3が認識したQRコードに書かれているGUIDと、直近の食べ物の3Dモデルのダウンロードリクエストの成否(表示内容はHTTP Status Code準拠)が表示されます。後述する `StandaloneMode` 時には、ファイル読み込み結果が表示されます。

- `Standalone Mode` を有効にすると、Yummy Control Serverに依存せずに事前に用意した食べ物を表示することができます。また、画面右側に `Standalone Foods` ウィンドウが表示されます。

  - `Standalone Foods` ウィンドウでは食べ物の名前が書かれたボタンを押すことで、食べ物を表示することができます。

> [!NOTE]
> `Standalone Mode` においても、食べ物の表示位置は **QRコードを認識した位置** になります。そのため、`Standalone Mode` 使用時は、食べ物を表示したい位置に **YummuVerse用の** 任意のQRコードを配置してください。

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

## 確認項目( `StandaloneMode` が有効)
1. `.glb` ファイルが所定のディレクトリに配置されているか？
  - `【運営向け】管理画面` の後半のStandaloneModeの説明を読んでください。

---
最終更新 : 2026/3/2

更新者 : Inoyu
