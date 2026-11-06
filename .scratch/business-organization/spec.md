# Organize application code by business responsibility

Status: Complete

Review feature/advice-receipts-sales (3555489) before integration: draft replacement/deletion, reviewable gRPC quotes with 409 refresh, price revalidation, Admin cancellations, immutable reverse stock/ledger/outbox, and React role controls. Main adds .NET9 and explicit advisory demo fixtures. Merge both histories without losing either; leave the existing GitHub Desktop stash untouched.

Preserve public HTTP contracts, business rules, EF model/migration IDs, stock transaction boundaries and root CLR namespaces. Replace the catch-all ShopService/IShopRepository with Catalogue, Trading, Inventory and Authentication responsibilities. Place controllers, DTOs, repository ports and implementations in their owning business folders inside existing layers; keep common paging/errors/mapping, EF persistence, gRPC and Redis integration explicit. Organize Domain entities, React feature pages/models/components and tests accordingly. No new application features or database migrations.

Baseline: branch has 30 passing net10 tests; integration with main has 31 passing net9 tests. Verify native build/tests, TypeScript build, EF model consistency, real Compose trading/advice workflows, docs and merge to main locally. Do not publish or push.

## Completion evidence

See docs/code-organization.md and docs/code-organization-checks.json. Final native build, 31 net9 tests, TypeScript/Vite, EF model consistency, Compose builds and 20 browser checks passed. Backend and frontend refactors were committed separately; branch histories are retained for the local merge to main.
