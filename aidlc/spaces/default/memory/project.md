# Project-Level Rules

## Tech Stack

- Unity Editor の基準は `ProjectSettings/ProjectVersion.txt` に記録された `6000.2.6f2` とする。
- 実装言語は C#。非同期処理は UniTask、リアクティブ処理は R3、依存性注入は Extenject を既存選択として尊重する。
- 対象は Meta Quest 3 を中心とする Android/OpenXR と PCVR、および iPad 等の外部 viewer。MR/Spatial Anchor、コントローラー操作、QR による anchor designation、生成食品 catalog、preview/model 取得、展示運用を主要境界として扱う。
- Unity Package の版は `Packages/manifest.json` と `Packages/packages-lock.json` を根拠にする。
- Network API は YummyService repository の normative v2 contract を根拠にし、採用 commit、OpenAPI version、checksum を intent/verification に記録する。

## Architecture

- `Assets/YummyVerse/Scripts/Model`, `ViewModel`, `View` の責務分離と、Interface 経由の依存方向を維持する。
- DI 登録は既存 Installer に集約し、ViewModel/Model から Unity View の具象へ直接依存させない。
- チュートリアルは ScriptableObject によるデータ駆動、イベント購読、`CancellationToken` の一括伝播という既存方針を維持する。
- Spatial Anchor、永続化、XR grab interaction、QR、ネットワーク、ファイル、入力、シーン、端末は統合境界として扱い、失敗と復旧動作を要件・テストに含める。
- 食べ物の識別と表示 pose を分離する。現行 target では食品 identity は生成履歴メニューの item ID から得て、QR はモデル出現 anchor の designation にだけ使う。QR payload/GUID を食品生成・選択・model download key に使用しない。
- 食べ物の表示 pose は、永続化した Spatial Anchor UUID と設定用 Cube の anchor-relative pose を単一の設定として扱う。
- メニュー一覧は preview image/metadata を先行取得し、全 3D model を一覧表示のために先行 load しない。
- Food interaction は game event の発火責任を持ち、Tutorial は FoodScooped/DishCleared を購読するだけにする。
- YummyService transport DTO は専用 v2 client boundary に隔離し、application domain は order/item identity と immutable artifact reference を使用する。
- Preview/GLB は artifact ID、revision、SHA-256、verified、selected pointer を検証し、固定 filename や QR GUID を cache identity にしない。
- Tutorial 完了後の一つの Virtual Menu は YummyService v2 item と Standalone local item を同時表示し、source-specific adapter から共通 food presentation flow へ接続する。
- Standalone Mode は API 非依存の第一級 source として維持し、Network/API failure から独立して local catalog/model を利用可能にする。
- 再設計の runtime root は `ProjectSettings/EditorBuildSettings.asset` で唯一 enabled の `Assets/YummyVerse/Scene/Restaurant.unity`、そこから再帰的に到達する Prefab/asset graph、Extenject の `NonLazy`/`IInitializable`、Unity lifecycle callback、Editor test とする。Scene/Prefab に付いているだけでは到達根拠としない。
- レイヤーの依存方向は `Domain contracts/value objects → Application/use cases → Infrastructure adapters / Presentation` とする。新しい依存は View → application port、Application → Domain/port、Infrastructure → port の実装、composition root → concrete に限定し、逆方向を追加しない。
- Domain/Application は `MonoBehaviour`、View concrete、network/filesystem/PlayerPrefs、Meta XR、glTF、input の具体実装を知らない。外部境界の port は利用者の役割ごとに定義し、local/remote を同じ汎用 interface の多重 bind で曖昧にしない。
- View 層の MonoBehaviour は serialized reference、Unity lifecycle、render/input event forwarding、`Update`/`LateUpdate` の tick forwarding だけを担う。UI 生成、network/file 処理、catalog policy、session/business decision、状態遷移、長い `switch`、購読所有判断は plain C# collaborator/use case に置く。`partial MonoBehaviour` への移動は薄型化とみなさない。
- View に公開する状態は原則 read-only stream/property とし、変更は command method で表す。subscription/cancellation/disposal の所有者を明記し、session または GameObject の lifetime と一致させる。
- Installer は composition root として feature registration へ委譲し、空 Installer と、scene に置かれているだけで登録を持たない component を禁止する。具象の `new`/bind は composition root に閉じ込める。
- transport DTO は mapper 境界で Domain/Application 型へ変換し、Network source と Standalone source の identity、failure、loading、lifecycle を混同しない。
- serialized asset を rename/move/delete する場合は `.meta` の GUID と Scene/Prefab/ScriptableObject/UnityEvent の参照を検証し、Unity load 検証まで完了条件に含める。
- 未使用コードは active runtime roots から code call、DI activation、Unity callback、serialized UnityEvent、ScriptableObject data reference のいずれでも到達しないものと定義する。Scene/Prefab に付いているだけでは使用根拠にならない。削除前に class reference、script GUID、active asset graph、tests/editor tooling を確認し、証拠を intent に残す。
- architecture gate として `FindObject`/service locator/static singleton は、device adapter 内の SDK 境界で避けられない場合を除き禁止する。例外は理由、期限、除去条件を intent/decision に記録する。

## Testing Posture

- 変更対象に対する EditMode または PlayMode テストを検討し、追加しない場合は理由と代替確認を `test-results.md` に残す。
- Unity Editor 上の自動テストと、Quest/PCVR の実機確認を混同しない。それぞれの結果と未実施理由を分けて記録する。
- 展示セッションのリセット、ユーザー離脱、Anchor の保存・復元失敗、保存データの欠落・破損、設定用 Cube の編集・固定、QR ロスト、通信失敗、ローカルファイル欠落を主要な回帰観点とする。
- QR の payload/認識を food identity source として再導入していないこと、designation 後の一時的ロストで食品が誤配置されないことを回帰確認する。
- Menu 表示時に未選択 3D model が load されないこと、scoop event の one-shot、visual/collider の段階縮小、crumb/disappear、DishCleared one-shot を回帰確認する。
- VR menu と physical viewer の item identity/状態整合性を、対象 device/transport が確定した後に確認する。
- YummyService v2 の全 OrderState/StageState/ArtifactType/ProblemDetails mapping、unknown enum、v1 rejection、SHA mismatch、stale response を contract test する。
- Contract test と production API integration を分け、v2 route/schema が公開済みでも placeholder deployment、preview/full-stage、artifact checksum、secret/TLS、runtime compatibility が未確認の状態を integration 合格にしない。
- active runtime roots、DI activation、Unity callback、serialized reference の到達性を、削除候補ごとに記録する。到達性監査をしていない削除は合格としない。
- Domain と Application の pure core/use case は EditMode unit test を基本とし、adapter の mapping、failure、timeout、cancellation は contract test で確認する。Scene/Prefab/ScriptableObject の変更は Unity Editor の load/参照検証を別に行う。
- Quest/Android/OpenXR、PCVR、Editor/Standalone の結果を一つにまとめない。未実行は `NOT-RUN` と記録し、成功扱いにしない。
- session/GameObject lifetime に紐付く subscription、CancellationToken、disposal を、正常終了・reset・destroy・再入場で検証する。Network と Standalone の片系 failure が他方を利用不能にしないことも確認する。

## Documentation

- 製品要件の規範的なソースは `aidlc/spaces/default/intents/`、intent をまたぐ安定知識は `aidlc/spaces/default/knowledge/aidlc-shared/` とする。
- チュートリアル、menu/model、QR/anchor、viewer、eating action の現行要件は `260824-guided-food-experience` を参照する。
- `docs/` は移管完了後の現行要件ソースとして参照しない。要件は source path の存在なしに理解できる本文と恒久 ID を `aidlc` に持たせる。
- 実装と `aidlc` の不一致を発見した場合、黙って片方を正とせず gap/未解決事項として記録する。
- `README.md` の Unity `6000.2.0f1` 表記と `ProjectVersion.txt` の `6000.2.6f2` は不一致のため、関連 intent で解消するまで既知の差分として扱う。
- 咀嚼計シリアル通信プロトコルの要点と Unity 側実装境界は `spaces/default/knowledge/aidlc-shared/chewing-sensor-serial-protocol.md` を参照する。v1.1 のキャリブレーション・フェーズ分割対応は `260904-chewing-calibration-phase-split` を参照する。

## Scope Overrides

- Unity が自動生成するファイル、Package キャッシュ、Build 成果物は設計対象または明示的な依頼でない限り変更しない。
- `.unity`, `.prefab`, `.asset` の変更はテキスト差分だけで完全性を断定せず、可能な範囲で Unity Editor のロードまたは検証結果を残す。

## Decided

- DECIDED: AI-DLC 成果物は V2 の space/intent モデルで管理し、旧 `aidlc-docs/` 構造を新規採用しない (2026-08-16)。
- DECIDED: 通常の作業 space は `default` とする (2026-08-16)。
- SUPERSEDED: 食べ物の表示 pose は QR Transform ではなく Spatial Anchor と anchor-relative pose で決定し、QR は通常モードの食べ物 GUID 入力として継続する (2026-08-21)。QR の食品 GUID 責務は 2026-08-24 の決定で上書きされた。配置 pose に関する部分は継続する。
- DECIDED: 設定確定時に Anchor UUID と設定用 Cube の anchor-relative pose を永続化し、次回起動時に復元する (2026-08-21)。
- DECIDED: 食品 identity は生成履歴の仮想メニュー item から得て、QR はモデル出現 anchor の designation のみに使う (2026-08-24)。
- DECIDED: メニュー preview は image/metadata のみを先行取得し、3D model data は選択 item に限定して cache/retrieve/load する (2026-08-24)。
- DECIDED: 現行要件は `aidlc` 内で自己完結させ、`docs/` の存在を要件理解の前提にしない (2026-08-24)。
- DECIDED: YummyVerseUnity が利用する API は YummyService v2 のみとする。v1 API は廃止済みであり、production/development/test/demo/fallback/migration/Standalone を含む全 runtime から金輪際呼び出さない。v1 rejection 用 local negative fixture だけを例外とする (2026-08-24)。
- DECIDED: Standalone Mode は今後も維持する。これは v1 fallback ではなく、API request を行わない端末内食品 source である。Tutorial 完了後の一つの食品選択 UI に YummyService v2 item と Standalone item を同時表示する (2026-08-24)。
- DECIDED: 咀嚼計シリアル通信プロトコルは v1.1 (`YummyVerse_Serial_Protocol_v1.1.md`) を採用し、キャリブレーションをノイズ測定・咀嚼測定のフェーズ分割コマンド (`CAL_NOISE`/`CAL_CHEW`) と中断コマンド (`CAL_ABORT`) へ移行する。フェーズ順序・再送・タイムアウトは Model 層 (`ChewingSensorService`)、案内表示・カウントダウンは Presentation 層 (`ChewingCalibrationFlow`) が持ち、両者は role-specific port `IChewingCalibrationPrompt` で接続する。咀嚼計の不調時にも展示を止めない既存方針は維持する。詳細は `spaces/default/knowledge/aidlc-shared/chewing-sensor-serial-protocol.md` と `260904-chewing-calibration-phase-split` を参照する (2026-09-04)。

## Forbidden

- NEVER `Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `UserSettings/` を設計上のソースまたはコミット対象として扱う。
- NEVER Unity の `.meta` と対応アセットの関係を無視して移動・削除する。
- NEVER Domain/Application から MonoBehaviour、View concrete、network/filesystem/PlayerPrefs、Meta XR、glTF、input の具体実装へ参照を張る。
- NEVER View MonoBehaviour に UI tree の生成、外部 I/O、catalog/session policy、business state transition、長い分岐、購読の lifetime 判断を置く。`partial` 化、helper MonoBehaviour の増設で規約を迂回しない。
- NEVER role-specific port を generic `IFetchable`/`IService` にまとめ、local と remote の具象を同じ曖昧な multi-bind にする。
- NEVER Installer を空のまま Scene component として残す、または composition root の外で具象を `new`/bind する。
- NEVER transport DTO、SDK type、raw JSON、route、PlayerPrefs key を Domain/Application の契約として漏らす。
- NEVER `FindObjectOfType`、service locator、static singleton を通常の解決経路に使う。device adapter 内の避けられない SDK 境界以外では使用しない。
- NEVER Scene/Prefab に attach されていることだけを理由に未使用コードを残す、または到達性の証拠なしにコードを削除する。

## Mandated

- ALWAYS シーンまたは Prefab の参照変更では、DI コンテナ境界と serialized reference の影響を確認する。
- ALWAYS 外部 endpoint と端末パスを扱うときは、タイムアウト、欠落、無効値、復旧を検討する。
- ALWAYS Spatial Anchor の作成、保存、読み込み、localization の各失敗を区別し、無効な保存 pose を world pose として黙って適用しない。
- ALWAYS Anchor を置き換える場合は UUID と anchor-relative pose を一貫して更新し、旧 Anchor の扱いを明示する。
- ALWAYS QR、menu item、preview、model data、anchor placement の各 identity/lifecycle を混同せず、変換境界を明示する。
- ALWAYS session reset 対象と session をまたいで保持する catalog/cache/placement を区別する。
- NEVER v1 API route/client/DTO/configuration/mock を runtime dependency として追加・維持する。`/v1/...` への outbound request と、v1/legacy への fallback を全面禁止する。Local negative fixture は v1 rejection test だけに限定する。
- ALWAYS v2 draft contract を更新するときは source commit/version/checksum と schema/path/security diff を review し、Unity Device の projection（status/artifact/payload/ACK）と public sample menu を区別する。
- ALWAYS downloaded artifact bytes の SHA-256 を確認してから decode/load/shared cache publish する。
- ALWAYS Network と Standalone の identity namespace、loading、error、availability を分離し、一方の失敗で他方を利用不能にしない。
- ALWAYS 新しい feature ごとに Domain/Application の責務、role-specific port、Infrastructure adapter、Presentation boundary、composition root の登録箇所を記録する。
- ALWAYS read-only state と command を分け、各 subscription の owner、開始条件、cancel 条件、disposal owner を実装と設計資料に明記する。UniTask/R3 は session または GameObject lifetime まで cancellation を伝播する。
- ALWAYS View の変更は forwarding に留め、UI 生成、I/O、policy、状態遷移を plain C# collaborator/use case へ抽出する。抽出先を `partial MonoBehaviour` にしない。
- ALWAYS port 追加時は consumer 側に役割固有の契約を置き、transport DTO は mapper で遮断する。Network と Standalone は別 adapter と別 failure policy を持つ。
- ALWAYS 削除候補について active runtime root からの code/DI/callback/UnityEvent/ScriptableObject の到達性を調べ、class reference、script GUID、asset graph、tests/editor tooling の証拠を `260828-architecture-redesign/audit/` または対応 decision に残す。
- ALWAYS architecture gate では許可された依存方向、具象 bind の位置、singleton/service locator の不在、空 Installer の不在を review checklist で確認する。
- ALWAYS 例外には一意 ID、理由、影響範囲、owner、期限、除去条件、代替テストを付け、intent/decision に期限付きで記録する。期限切れの例外は新規変更の合格条件を満たさない。
- ALWAYS この恒久規約の詳細と実例は `aidlc/spaces/default/knowledge/aidlc-shared/architecture-and-code-quality.md`、今回の適用計画と監査証拠は `aidlc/spaces/default/intents/260828-architecture-redesign/` に記録する。

## Corrections

<!-- プロジェクト固有の承認済み学習だけを追記する。 -->
