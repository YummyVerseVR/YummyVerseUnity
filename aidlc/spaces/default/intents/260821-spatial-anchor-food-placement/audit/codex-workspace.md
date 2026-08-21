# Audit Shard: Codex Workspace

## 2026-08-21 Intent Authorization

- Actor: 利用者
- Decision: 食べ物の表示位置を QR コード基準から Spatial Anchor 基準へ変更する。
- Requested capabilities: controller button で開く設定画面、Spatial Anchor の設定、controller で grab 可能な Cube、Cube 位置による食べ物表示位置の固定。
- Authorization: 要件更新と実装を明示的に依頼。

## 2026-08-21 Scope Interpretation

- Decision: QR payload/GUID による通常モードの食べ物選択は維持し、QR Transform だけを表示 pose から切り離す。
- Reason: 利用者の変更対象は「食べ物の表示位置」であり、QR flow を全廃すると食べ物 GUID の入力源まで失われるため。
- Decision: Anchor UUID と Cube の anchor-relative pose を schema version 付きで永続化し、次回起動時に復元する。
- Decision: 初回設定は、draft Cube の現在 world pose への Anchor 作成・保存と、Anchor 保存後に Cube だけを動かす food relative pose 確定の二段階とする。
- Decision: 通常の game/session reset は配置設定を保持し、明示的な運営操作だけが再編集・置換を行う。
- Remaining choice: Anchor の完全削除を UI に露出するか、置換だけを提供するかは実装・運用レビューで確定する。

## 2026-08-21 Documentation Changes

- Shared project context と project/operation guardrails の QR 位置依存を Spatial Anchor 方式へ更新。
- Intent record に intent、requirements、domain components、ADR、traceability、phase checks を追加。
- Application code、Unity scene、Prefab、Asset はこのドキュメント作業では変更していない。
