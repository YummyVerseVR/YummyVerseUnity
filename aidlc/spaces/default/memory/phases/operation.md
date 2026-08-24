# Operation Phase Guardrails

## Deployment

- 配布対象（Quest APK、PCVR build）、版、commit、設定値、配布先を記録する。
- 更新前の復旧手段と、更新後の smoke test を定義する。
- 展示現場のネットワーク有無と Standalone Mode の必要ファイルを確認する。

## Observability

- Spatial Anchor の作成・保存・読み込み・localization、QR による anchor designation、表示 pose の確定・復元、catalog/menu selection、preview/model 取得、scoop/consumption、physical viewer、セッション中断、リセットの診断情報を確認可能にする。
- API operation では v2 contract revision、order/item ID、OrderState/StageState、artifact ID/revision/type/verified、integrity result、problem type/status、retry/cancel を相関可能にする。Token、authorization header、signed URL は記録しない。
- Deployment 前に v1 outbound route が0件であることと、v2 compatibility/auth/TLS/artifact download contract が満たされることを確認する。
- ログに秘密情報や不要な個人情報を含めない。

## Incident Response

- Runbook は運営者が現場で実行できる具体的な確認順序、復旧、エスカレーション条件を持つ。
- 端末再起動で失われる設定や実験的機能を明示する。
- 保存済み Anchor が復元できない場合の再設定手順と、Anchor 設定を意図的に削除・置換する手順を明示する。

## Corrections
