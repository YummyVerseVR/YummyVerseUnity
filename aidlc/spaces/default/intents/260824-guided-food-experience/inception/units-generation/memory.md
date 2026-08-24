# Units Generation Memory

- 現行 intent は全体として Construction `NOT-READY` だが、利用者が確定要件だけの部分実装を明示的に依頼した。
- API domain vocabulary と fail-closed rules は transport contract 非依存のため独立 Unit にできる。
- v1/旧 GUID route の撤去と QR/food identity 分離は、未解決の endpoint や anchor algorithm を必要としない。
- Consumption の残量単調減少と完食 one-shot は、AABB 算法を決定せず pure domain state として先行できる。
- CodeKB は空のまま維持し、今回の限定 inventory を完全 reverse engineering と表現しない。
