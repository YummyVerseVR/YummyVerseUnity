# Requirements

## Historical Scope and Supersession

この requirements は 2026-08-21 の Spatial Anchor 変更を記録する。`FR5` のうち QR payload/GUID を食品取得トリガーとして継続する要件、`NFR5` の QR GUID→GLB 契約維持、QR による food GUID 選択を Out of Scope とした判断は、`260824-guided-food-experience` の `FR13`、`FR16`、`ADR-003` により superseded された。現行要件では食品 identity は生成履歴メニュー item から得て、QR は anchor designation のみに使用する。Anchor UUID、relative pose、復元、安全な失敗、session reset で配置を保持する要件は継続する。

## Intent Analysis

- 目的: QR の検出 Transform に依存せず、展示運営者が設定・保存した Spatial Anchor 相対位置へ食べ物を安定して表示する。
- 対象ユーザー: 展示運営者、来場者。
- 成功指標: 再起動後の復元、QR 移動・ロストからの位置独立、復元失敗時の安全な再設定を acceptance criteria で確認できること。

## Functional Requirements

- **FR1: コントローラーから設定画面を開閉できること**
  - 設定画面は既存の設定画面表示用 controller action で表示・非表示を切り替える。
  - 表示時は操作者から読め、コントローラーで操作可能な位置へ配置する。
  - 検証条件: 右手コントローラーの設定ボタンを押すたびに、設定画面の表示状態と raycast/interactable 状態が一致して反転する。
  - 出典: 利用者要求、`Assets/YummyVerse/Scripts/Model/InputLayer.cs`、`Assets/YummyVerse/Scripts/View/UI/ConfigUIView.cs`。

- **FR2: 設定画面から Spatial Anchor を作成・保存・置換できること**
  - Anchor 未設定時は設定画面に draft Cube を表示し、操作者が controller で Anchor を置きたい world pose へ移動できる。
  - `Set/Replace Anchor` 操作時は draft Cube の現在 world pose に新しい Spatial Anchor を作成して保存する。
  - 設定画面は Anchor 未設定、作成中、保存済み、復元中、使用可能、失敗の状態を区別する。
  - Anchor の作成・保存に成功した場合だけ、その UUID を有効な配置設定として採用する。
  - 既存 Anchor の置換では、新 Anchor の保存と新しい配置設定の永続化が成功する前に、利用可能な旧設定を失わない。
  - 検証条件: Anchor 設定前に Cube を移動し、設定操作を行うとその world pose に有効な Anchor UUID が得られる。失敗時は確定状態にならず再試行でき、置換失敗時は旧設定を引き続き利用できる。
  - 出典: 利用者要求、実装判断 `[D1]`。

- **FR3: 設定用 Cube をコントローラーで掴んで表示 pose を調整できること**
  - 配置編集状態では、左右いずれかの対応コントローラーで Cube を grab して移動・回転できる。
  - Cube は使用可能な Spatial Anchor の子座標系で扱い、その local position と local rotation を食べ物の anchor-relative pose とする。
  - Anchor 保存後に Cube を移動しても Spatial Anchor 自体の world pose は変更しない。
  - Cube は設定画面が表示され、配置編集が有効な間だけ表示・grab 可能とする。
  - 検証条件: Anchor の world pose を記録してから Cube を grab すると Cube の local pose だけが変わり、Anchor の world pose は変化しない。設定画面を閉じるか配置を確定すると grab できなくなる。
  - 出典: 利用者要求、実装判断 `[D1]`。

- **FR4: Cube の位置を食べ物の表示 pose として確定できること**
  - 使用可能な Spatial Anchor が存在する場合だけ、設定画面から表示位置を確定できる。
  - 確定時は Cube の anchor-relative pose を保存し、食べ物表示へ即時反映する。
  - 未設定または復元失敗中は、未検証の world pose に食べ物を表示しない。
  - 検証条件: 確定後の食べ物の position/rotation が Cube の world pose と一致し、Cube を編集できない状態になる。
  - 出典: 利用者要求。

- **FR5: QR Transform を食べ物の表示 pose に使用しないこと**
  - 通常モードでは QR payload の GUID を食べ物取得トリガーとして継続利用する。
  - QR trackable の Transform、追跡更新、ロスト通知は食べ物の position/rotation を更新してはならない。
  - Standalone Mode も同じ Spatial Anchor 配置設定を使用する。
  - 検証条件: 同じ食べ物を表示中に QR を移動・回転・ロストしても食べ物の world pose が変化しない。通常モードの GUID による取得と Standalone Mode のローカル取得は継続する。
  - 出典: 利用者要求、`Assets/YummyVerse/Scripts/ViewModel/FoodViewModel.cs`、`Assets/YummyVerse/Scripts/View/FoodView.cs`。

- **FR6: Anchor UUID と anchor-relative pose を永続化すること**
  - 保存データは schema version、Anchor UUID、Cube の local position、local rotation、配置確定状態を一つの論理レコードとして保持する。
  - Spatial Anchor 本体の保存成功後にのみ、その UUID をアプリケーション設定へコミットする。
  - 検証条件: 保存データを読み戻すと同じ UUID、position、rotation、確定状態が復元される。部分的または破損したデータは有効扱いにしない。
  - 出典: 実装判断 `[D1]`。

- **FR7: 次回起動時に保存済み配置を復元すること**
  - 起動時に保存済み Anchor UUID を読み込み、Spatial Anchor の load/localization 成功後に Cube の anchor-relative pose を適用する。
  - 復元が完了するまで配置を使用可能とみなさない。
  - Anchor が見つからない、load/localization に失敗する、または保存データが無効な場合は再設定可能な状態へ遷移し、失敗理由を設定画面とログで確認できるようにする。
  - 検証条件: アプリケーションを再起動して同じ物理空間で localization すると、食べ物が保存前と同じ Anchor 相対 pose に表示される。復元失敗時には食べ物を古い world pose に表示しない。
  - 出典: 実装判断 `[D1]`。

- **FR8: 配置を再編集・再確定できること**
  - 確定済み配置を設定画面から編集状態へ戻し、Cube を再配置して確定できる。
  - 同じ Anchor 内の Cube 再配置では UUID を変更せず relative pose のみ更新できる。
  - Anchor を置換した場合は新 UUID と新 relative pose を一貫した組として保存する。
  - 検証条件: 再編集後の確定値が即時反映され、再起動後も新しい値が復元される。
  - 出典: 利用者要求、実装判断 `[D1]`。

- **FR9: 通常のセッションリセットで配置設定を消去しないこと**
  - 来場者向けの game/session reset は、保存済み Spatial Anchor と食べ物表示 pose を保持する。
  - 配置の消去または Anchor の置換は、設定画面上の明示的な運営操作に限定する。
  - 検証条件: セッションリセット後も同じ配置を利用でき、明示的な消去操作の後だけ未設定状態へ戻る。
  - 出典: 展示運用上の実装判断 `[D1]`。

- **FR10: 配置状態と操作結果を設定画面で確認できること**
  - 設定画面は少なくとも Anchor の有無、復元・保存処理中、配置編集/確定、最後の失敗を表示する。
  - Anchor 未使用時に確定操作を押せないなど、現在の状態で無効な操作を抑止する。
  - 検証条件: 各状態遷移に応じて表示と操作可否が更新され、失敗後に再試行または再設定へ進める。
  - 出典: 利用者要求から導出した運用要件。

## Non-Functional Requirements

- **NFR1: 対応環境を偽らないこと**
  - Meta Quest 3 の Spatial Anchor provider を本番対象とする。Anchor API を利用できない Editor/PCVR 環境では、永続 Anchor が利用可能であるかのように確定成功を表示しない。
  - 検証方法: Quest 実機確認と、Anchor provider が利用できない環境での失敗経路確認を分けて記録する。

- **NFR2: 永続化の整合性を保つこと**
  - Anchor UUID と anchor-relative pose は同じ schema version のレコードとして検証し、書き込み途中または形式不正の値を適用しない。
  - 検証方法: 正常値、UUID 不正、pose 欠落、schema version 不一致を入力した復元テスト。

- **NFR3: 非同期 lifecycle を安全に処理すること**
  - Anchor の作成・保存・読み込み・localization は Unity lifecycle と cancellation を考慮し、破棄済み GameObject への反映や多重操作による状態競合を起こさない。
  - 検証方法: 処理中の画面非表示、シーン終了、連打を含む PlayMode/実機確認。

- **NFR4: 既存の XR 操作性を維持すること**
  - 設定画面と Cube は既存の controller/ray/grab interaction と競合せず、配置確定後は来場者の通常操作を妨げない。
  - 検証方法: 設定画面の UI 操作、Cube grab、設定終了後の Food destroy/start 操作を Quest 実機で確認する。

- **NFR5: 既存の食べ物取得経路を維持すること**
  - ネットワーク接続時の QR GUID → GLB download と、Standalone Mode の端末内 file load の契約を変更しない。
  - 検証方法: 通常モードと Standalone Mode の双方で、配置確定後に食べ物を表示する回帰確認。

## Constraints

- Unity `6000.2.6f2`、Meta XR SDK `78.0.0`、XR Interaction Toolkit `3.2.1` を現行基準とする。
- Model/ViewModel/View の責務分離と Extenject による DI を維持する。
- Spatial Anchor は Meta SDK の正式表記とし、単一 Anchor・単一 food placement を対象とする。
- Spatial Anchor の保存可否と localization は Quest の runtime/permission/physical-space 状態に依存する。

## Out of Scope

- QR による food GUID 選択の廃止。
- 複数 Anchor、複数表示位置、cloud 共有、端末間同期。
- 食べ物データ形式、API endpoint、Food Scale の変更。
- 展示会場間で同じ UUID を移送して利用できることの保証。

## Sources

- 2026-08-21 利用者要求: QR ベースの表示位置から Spatial Anchor ベースへ変更し、設定画面、controller grab 可能な Cube、位置確定を追加する。
- `[D1]`: 2026-08-21 実装判断。Anchor UUID と Cube の anchor-relative pose を永続化し、次回起動時に復元する。
- `Assets/YummyVerse/Scripts/View/FoodView.cs`: 現行は QR Transform へ毎フレーム追従する。
- `Assets/YummyVerse/Scripts/ViewModel/FoodViewModel.cs`: 現行は QR detection service から food Transform を受け取る。
- `Assets/YummyVerse/Scripts/Model/FoodContext.cs`: QR GUID を食べ物取得トリガーとして利用する。
- `Assets/YummyVerse/Scripts/Model/InputLayer.cs`: controller action から設定画面表示イベントを発行する。
- `Assets/YummyVerse/Scripts/View/UI/ConfigUIView.cs`: 現行設定画面を camera 前へ表示する。
- `Packages/manifest.json`: Meta XR SDK、XR Interaction Toolkit、OpenXR の package versions。

## Assumptions & Open Questions

- 確定: 設定画面表示中の draft Cube を controller で配置し、Anchor 設定操作時の Cube world pose に新 Anchor を作成する。保存後は Cube だけを動かし、Anchor との relative pose を食べ物位置として確定する。
- 仮定: 通常のセッションリセットは展示キャリブレーションを保持し、明示的な運営操作だけが配置を削除する。
- Open Question: Anchor の完全削除操作を初回実装の設定画面へ露出するか、Anchor の置換操作に限定するか。

## Review

- Status: `READY`
- Approval basis: 2026-08-21 の利用者による変更・実装要求、および `[D1]`。
- Reviewed at: 2026-08-21
