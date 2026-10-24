# Seed reviewable catalogue and trading data

Status: Complete

Implement an explicit seed CLI/script, idempotent/resumable records, stock via Service confirmations, configurable Employee credentials, documentation and integration/browser evidence.

## Comments

2026-10-24: Applied 8 products/16 SKUs/3 suppliers plus 3 receipts/4 sales and Employee. Confirmations create stock/ledger/outbox via Service/gRPC. Integration test resumes an interrupted import and preserves later staff changes. Actual replay and read-only Admin/Employee browser checks pass. Credentials remain in ignored .env.
