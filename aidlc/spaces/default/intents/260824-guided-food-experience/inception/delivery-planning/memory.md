# Delivery Planning Memory

- 利用者承認は未決定事項の解決や full Construction gate の通過へ拡張しない。
- 旧 v1 runtime への rollback は製品決定に反するため、通常の rollback option に含めない。
- Unity が開かれている環境では別 batchmode instance を競合起動せず、既存 Editor の refresh/compile と test API を使う。
