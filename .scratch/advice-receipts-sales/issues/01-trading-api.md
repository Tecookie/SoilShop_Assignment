# Finish transactional receipt and sale APIs

Status: Complete

Implement draft update/delete, normalized line inputs, quote, version-checked
confirmation and Admin cancellation. Preserve all-or-nothing inventory, ledger,
outbox, historical prices, action idempotency and linked-suggestion safety.

## Comments

Verify against isolated PostgreSQL, including concurrency and rollback.

2026-11-05: Verified by the 30-test application suite. No new table/migration;
existing document locks, cancellation fields, ledger indexes and outbox reused.
