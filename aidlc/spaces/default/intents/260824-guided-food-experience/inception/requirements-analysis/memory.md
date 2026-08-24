# Requirements Analysis Memory

- 旧チュートリアル仕様の QR event は `QrPlateDetected` だったが、新要求では食品 GUID 選択ではなく anchor designation の成立イベントとして扱う必要がある。
- S15〜S19 は旧仕様どおり TutorialSequence の外に置く一方、要件上は一つの来場者 journey として追跡した。
- 利用ガイドにだけ記載されていた `IGameCommandBus`、reset 対象、debug HUD、dummy event の撤去条件も、実装の偶然ではなく要件を守る境界として採用した。
- 画像先行読込と「即座に呼び出す」は両立させる必要がある。一覧表示は image/metadata のみ、3D は選択 item の cache 再利用または on-demand load としたが、数値 SLA は未定である。
- 外部 viewer が preview だけか 3D を含むかは決めず、共通 item identity と閲覧可能性だけを baseline にした。
- AABB の技術用語と「最遠2点」の組合せには解釈余地があるため、要求文を保持しつつ algorithm を open question にした。
- 食べカス、縮小、消滅は簡易表現であり、断面生成は明示的に除外した。
