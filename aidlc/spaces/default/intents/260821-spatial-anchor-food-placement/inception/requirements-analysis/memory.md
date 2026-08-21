# Requirements Analysis Memory

- 既存コードでは QR が「GUID による食べ物選択」と「Transform による表示 pose」の二つの責務を持つ。今回変更するのは後者であり、前者は互換性維持の対象とした。
- 表示位置だけでなく Cube の rotation も自然な controller grab で変化するため、永続化対象は position ではなく position/rotation を含む pose とした。
- 「固定」は現在セッションの transform 固定だけではなく、Anchor UUID と relative pose の永続化、および次回起動時の復元までを含むものとした。
- 初回設定は二段階とした。まず draft Cube の現在 world pose に Anchor を作成・保存し、その後 Anchor は動かさず Cube を移動して food relative pose を確定する。
- 復元失敗時に前回の world pose を流用すると誤った物理位置へ食べ物が出るため、未設定/要再設定へ戻すことを要件化した。
