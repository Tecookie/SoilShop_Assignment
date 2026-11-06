# Complete approved advice and counter trading workflows

Status: Complete

Scope: system-design sections 5.2–5.4, with 5.5 transactional guarantees.
Reuse the existing source-reviewed advice-v2 implementation and its tests.
Do not add customer accounts, payments, returns, a new design system or unrelated operations.

Complete receipt/sale draft replacement and deletion, duplicate-SKU merging,
whole-VND quotes through gRPC, per-line price revalidation at confirmation,
and Admin whole-document cancellation with immutable snapshots, reverse ledger
and outbox in one PostgreSQL transaction. Maintain expected versions/action keys.
Unknown receipt costs may remain in drafts. A stale expected total returns 409
with a refreshed quote for explicit review; unavailable gRPC never changes stock.

React extends the existing TradingPage form/table/components and keeps pending,
errors, retry action keys and role controls explicit. No database schema change
is expected: cancellation and ledger columns already exist.

Documentation follows existing English docs: feature guide under docs/,
dated evidence in docs/verification.md, and accurate pending work in
docs/implementation.md. Do not mark previous owners' pending workflows complete.

## Comments

Baseline c4c1a77 already includes advice-v2 and receipt/sale confirmation.
Known gaps: draft CRUD, cancellation, reviewable quote and per-line price race check.

2026-11-05: Completed those baseline gaps. Thirty application tests pass;
six new trading, nine original trading and five advice browser checks pass on
real Compose. See docs/trading-workflows.md and the dated docs/verification.md.
