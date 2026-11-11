# Complete approved shop workflows

Status: Complete

Implement stock counts and version-safe Admin approval, direct reasoned adjustments,
restock-to-receipt conversion, dated summaries, serialized PostgreSQL backups with
seven-success retention, supplier/account administration and searchable selectors.
Preserve existing layered business ownership, stock transactions and historical data.
No storefront, payment, generic repository or WCF expansion. Do not publish.

## Acceptance

- Stock changes, ledger and outbox commit atomically; stale counts cannot overwrite sales.
- Conversion is idempotent and does not add stock.
- Daily Vietnam-time jobs catch up after restart; manual triggers and results are visible.
- Backups use PostgreSQL 16 tools, validate archives and support an isolated restore drill.
- Admin boundaries and last-active-Admin protection are tested.
- Existing workflows and new screens pass backend/build/browser checks.

## Comments

- User authorized all remaining improvements on 2026-11-11.
- Implemented and verified on isolated PostgreSQL and Compose fixtures; see `docs/verification.md`. Final assignment presentation and named team responsibilities remain team tasks.
