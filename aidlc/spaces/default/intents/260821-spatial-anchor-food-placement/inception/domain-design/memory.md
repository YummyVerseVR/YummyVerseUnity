# Domain Design Memory

- 食べ物選択と配置を分離するため、QR GUID の既存 subscription は維持し、QR Transform → FoodViewModel の subscription と FoodView の QR 追従を置き換える。
- 設定用 Cube は、Anchor 作成前には Anchor world pose の draft、Anchor 保存後には food anchor-relative pose の draft という二つの役割を状態で切り替える。Cube を動かしても保存済み Anchor 自体は動かさない。
- relative pose の rotation と glTF モデル固有の表示補正 rotation/scale は異なる階層で適用し、保存値がモデル補正で上書きされない構造が必要である。
- Anchor の置換は新設定の成功後に active UUID を切り替える。旧 Anchor の erase 失敗は新設定を rollback する理由にはせず、cleanup warning とする。
- Quest 実機でのみ確認できる Anchor save/load/localization と、Editor で検証できる状態・永続化ロジックを分離して検証する。
