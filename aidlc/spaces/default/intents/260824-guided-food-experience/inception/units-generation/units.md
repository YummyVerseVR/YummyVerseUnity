# Unit Definition

## Scope Decision

2026-08-24 の利用者指示「現在の `aidlc` で決まっている要件だけで実装可能な部分を実装する」を、全 Construction gate の一括通過ではなく、未解決事項に依存しない Unit だけを開始する明示承認として扱う。`Q1`〜`Q11` や当時未公開だった HTTP contract を実装判断で補完しない。2026-08-30 の `ru322/main` refresh 後は、公開された Unity Device schema を根拠に Unit 04/05 の設計を更新する。

## Units

| Unit ID | Unit | Requirements | Readiness | Boundary |
|---|---|---|---|---|
| `UNIT-01` | YummyService v2 domain contract foundation | `FR25`, `FR26`, `FR29`, `FR31`, `NFR10`, `NFR11` | `READY` | 既知 enum、opaque identity、immutable artifact reference、selectable gate、v1/unknown fail-closed。HTTP path/auth/download は含めない |
| `UNIT-02` | Food identity runtime separation and v1 retirement | `FR13`, `FR16`, `FR25`, `FR34`, `FR35`, `NFR7`, `NFR11` | `READY` | QR change から food load を起動せず、Standalone menu selection を local load 起点にする。旧 outbound client/config を除去または fail-closed にする |
| `UNIT-03` | Food consumption state foundation | `FR21`, `FR22`, `FR23`, `FR24`, `NFR4`, `NFR9` | `READY` | 1 action ごとの単調減少、0 下限、完食 one-shot の純粋状態。AABB、scoop 判定、scale/effect View への接続は含めない |
| `UNIT-04` | Unified catalog and Virtual Menu | `FR12`〜`FR14`, `FR24`, `FR34`, `FR35`, `NFR5`, `NFR7` | `NOT-READY` | History route/schema は公開済み。preview operation、order change/cache policy、表示順、同名表示、placeholder policy は未確定 |
| `UNIT-05` | Selected model/artifact delivery | `FR29`〜`FR31`, `NFR5`, `NFR12`〜`NFR14` | `NOT-READY` | Device artifact download は公開済み。Unity checksum/revision metadata、size/redirect/range/retry、`Q1`/`Q10` が未解決 |
| `UNIT-06` | QR anchor designation integration | `FR16`, `FR17`, `FR24`, `NFR7` | `NOT-READY` | `Q5` の既存 Spatial Anchor/Cube flow との優先関係待ち |
| `UNIT-07` | Food bounds, scoop, and effects integration | `FR18`〜`FR23`, `NFR6`, `NFR9` | `READY` | `Q3` 解決済み (2026-08-28)。透明 AABB collider、手/コントローラーとの接触によるすくい正規化、段階縮小と消滅、共通イベント発行まで |
| `UNIT-08` | Physical Menu Viewer | `FR15`, `FR27`〜`FR33`, `NFR6`, `NFR13` | `NOT-READY` | Unity Device routeは参照できるが、`Q2`/`Q7` の viewer scope・preview・transport/auth は未確定 |

## Dependency Graph

```text
UNIT-01 ──> UNIT-04 ──> UNIT-05
                 └────> UNIT-08

UNIT-02 ──> UNIT-04
      └────> UNIT-06

UNIT-03 ──> UNIT-07
```

`UNIT-01`〜`UNIT-03` は相互に独立して Construction 可能である。後続 Unit が `NOT-READY` の間、先行 Unit を production API integration、統合 menu、実 food interaction の完成と扱わない。

## Brownfield Coverage

今回の差分分析は次に限定した partial scan であり、完全な CodeKB reverse engineering ではない。

- `Assets/YummyVerse/Scripts/Model`, `ViewModel`, `View`, `Editor`
- `Assets/YummyVerse/Prefabs/Restaurant/UI/YummyConfigUI.prefab`
- 旧 outbound route の残存確認に必要な `Assets/FoodDB/Scripts`
- `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`
- 本 intent の requirements、domain design、contract design、shared knowledge

CodeKB の九成果物は今回作成せず、完全調査を実施したとは主張しない。

## Review

- `UNIT-01`〜`UNIT-03`: `READY`
- `UNIT-04`〜`UNIT-08`: `NOT-READY`
- Full intent Construction: `NOT-READY`
