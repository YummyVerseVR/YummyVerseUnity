# YummyVerseUnity Project Context

## Product

YummyVerse は、食感再現を目的とした展示型 VR/MR アプリケーションである。QR コードを検出した位置に食べ物の 3D モデルを表示し、ネットワーク経由または端末内の Standalone データから体験を開始する。

## Primary Actors

- 来場者: QR を見て食べ物を表示し、VR/MR 体験を行う。
- 展示運営者: endpoint、Food Scale、Standalone Mode、接続・読込状態を管理する。
- 開発者: Unity、Quest、PCVR、外部 Yummy Control Server 間の統合を保守する。

## Main Runtime Flow

1. 入力と QR 検出を受け取る。
2. QR の GUID を検証する。
3. 通常モードでは Yummy Control Server から GLB を取得し、Standalone Mode では端末内ファイルを読み込む。
4. glTF をロードして QR 検出位置へ食べ物を表示する。
5. チュートリアル、FreePlay、運営 UI、リセットが同じ展示セッションを制御する。

## Architectural Landmarks

- `Assets/YummyVerse/Scripts/Model/`: 状態、入力、QR、ネットワーク、食べ物、イベント、リセット。
- `Assets/YummyVerse/Scripts/ViewModel/`: UI/ゲーム表示の調停とチュートリアル進行。
- `Assets/YummyVerse/Scripts/View/`: Unity `MonoBehaviour` と表示・端末境界。
- `Assets/YummyVerse/Scripts/*/DI/`: Extenject Installer。
- `Assets/YummyVerse/Data/Tutorial/`: ScriptableObject ベースの条件、step、sequence、localized data。
- `Assets/YummyVerse/Scripts/Tests/`: 現在確認できる Unity テスト領域。

## External and Device Boundaries

- Meta Quest 3 / OpenXR / Meta XR SDK
- QR trackable と MRUK
- Yummy Control Server endpoint
- HTTP による GLB download
- Android `Application.persistentDataPath` 配下の Standalone food files
- Unity Localization、Addressables、glTFast

## Known Documentation

- `README.md`: ビルド、利用方法、運営 UI、トラブルシューティング。
- `docs/tutorial-requirement.md`: チュートリアルの設計・実装要件。
- `docs/tutorial-usage.md`: チュートリアルの Unity Editor セットアップと拡張方法。

## Known Gaps

- `README.md` の推奨 Unity 版と `ProjectSettings/ProjectVersion.txt` の実版が一致していない。
- 自動テストの網羅性、CI、Quest/PCVR の再現可能な実機テスト手順は、今後の intent で確認・補強が必要。
- 外部サーバー契約の正式な API 文書は、このリポジトリ内の現行 Markdown からは確認できない。
