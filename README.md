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
開いたシーンでQRコードを見つめていれば食べ物が出てきます。
2026年2月の更新で、シーンを再読み込みしなくても読み込むQRを変えることで他の食べ物を体験できるようになりました。

# トラブルシューティング
2026年2月の更新で管理画面がつき、モデルダウンロード時のHTTP Status Codeが確認できるようになる予定です。(2026/2/16時点では未実装です)

---
最終更新 : 2026/2/16

更新者 : Inoyu