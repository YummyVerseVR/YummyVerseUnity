# Domain Design Memory

- 現行 tutorial code/assets の存在は repository inventory で確認したが、新しい QR/Menu/Eating 要件への適合性は未検証である。
- Tutorial の既存 POCO + Extenject 構成、追加 Presenter、S1 を Attract config が担当する差分は、要求される責務分離と data-driven behavior を満たす限り維持できる。
- QR role の変更は name replacement だけではない。`FoodContext`/download trigger、MenuSelectionBridge、QrPlateDetected condition asset、S3 文言、reset/test flow の契約 migration が必要になる。
- Catalog item を preview と model reference に分けることで、VR menu と physical viewer の共通 contract を最小化した。
- AABB は model load 後に一度計算し、以後は food root の transform/scale に追従させる方向が低負荷だが、最遠2点の定義は未決定である。
- DishCleared は最終 visual cleanup と event の重複防止を一つの consumption owner が管理する必要がある。
- YummyService v2 は state/schema と transport を分けて扱う。OpenAPI `paths` が空の間は client interface/fixture の設計だけを READY とし、実 endpoint integration を READY にしない。
- API DTO と application catalog を専用 adapter で分離し、Standalone local item は別 adapter から同じ `GeneratedFoodItem` へ変換する。
- Preview/GLB の identity は v2 `ArtifactRevision` と selected pointer を基準にし、QR GUID と mutable filename を排除する。
- Contract が `COMPLETED` order 前の customer artifact visibility を決めていないため、現設計は全 completion gate 後に Ready とする。変更には `Q11` の明示決定が必要である。
