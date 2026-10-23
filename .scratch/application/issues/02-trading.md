# Receive and sell bags safely

Status: Complete

Create receipt/sale drafts and confirm through Service/Repository with independent gRPC totals. Acceptance: last-bag concurrent confirmation has one winner, duplicate confirmation has one effect, multi-line shortage rolls back ledger/outbox/stock.

## Comments

2026-10-23: Twelve application tests pass, including real PostgreSQL concurrency, rollback, idempotency, frozen sale prices and input/auth checks. API integration tests replace only the network calculation boundary; actual gRPC communication is checked separately with Compose.
