# YummyVerseUnity Project Context

## Product

YummyVerse は、食感再現を目的とした展示型 VR/MR アプリケーションである。食べ物の 3D モデルは、展示運営者が設定した Spatial Anchor を基準に固定した位置へ表示し、ネットワーク経由または端末内の Standalone データから体験を開始する。QR コードは通常モードで食べ物を識別する GUID の入力に使うが、表示位置・姿勢の決定には使わない。

## Primary Actors

- 来場者: 食べ物を選択し、Spatial Anchor を基準に固定された位置で VR/MR 体験を行う。
- 展示運営者: コントローラーから設定画面を開き、Spatial Anchor、食べ物の表示位置、endpoint、Food Scale、Standalone Mode、接続・読込状態を管理する。
- 開発者: Unity、Quest、PCVR、外部 Yummy Control Server 間の統合を保守する。

## Main Runtime Flow

1. コントローラー入力を受け取り、設定画面を表示する。
2. 保存済み Spatial Anchor を復元するか、設定用 Cube の現在位置に Spatial Anchor を作成して保存する。
3. 展示運営者がコントローラーで Cube を掴んで移動し、Spatial Anchor 相対の食べ物表示 pose を確定・保存する。
4. 通常モードでは QR の GUID を検証して Yummy Control Server から GLB を取得し、Standalone Mode では端末内ファイルを読み込む。
5. glTF をロードし、保存された Spatial Anchor 相対 pose へ食べ物を表示する。QR の Transform は表示 pose に利用しない。
6. チュートリアル、FreePlay、運営 UI、リセットが同じ展示セッションを制御する。

## Architectural Landmarks

- `Assets/YummyVerse/Scripts/Model/`: 状態、入力、Spatial Anchor、表示位置の永続化、QR、ネットワーク、食べ物、イベント、リセット。
- `Assets/YummyVerse/Scripts/ViewModel/`: UI/ゲーム表示の調停とチュートリアル進行。
- `Assets/YummyVerse/Scripts/View/`: Unity `MonoBehaviour` と表示・端末境界。
- `Assets/YummyVerse/Scripts/*/DI/`: Extenject Installer。
- `Assets/YummyVerse/Data/Tutorial/`: ScriptableObject ベースの条件、step、sequence、localized data。
- `Assets/YummyVerse/Scripts/Tests/`: 現在確認できる Unity テスト領域。

## External and Device Boundaries

- Meta Quest 3 / OpenXR / Meta XR SDK
- Meta XR Spatial Anchor と永続化された Anchor UUID
- XR Interaction Toolkit によるコントローラー操作と設定用 Cube の grab interaction
- 通常モードにおける食べ物 GUID 取得用の QR trackable と MRUK（位置・姿勢には不使用）
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
