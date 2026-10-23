# Demonstrate low-stock events and web workflow

Status: Complete

Implement API Redis outbox relay, Worker consumer/reconciliation and low-stock inbox. Build React login, inventory/catalogue and trading screens. Acceptance: six services run in Compose; browser receipt -> sale -> updated stock; Worker creates one active suggestion and handles duplicate events.

## Comments

2026-10-23: Six services run locally. Browser smoke passed nine checks: catalogue creation, receive6/sell2/current4, one active target30/current4 suggestion26, two ledger movements, session flows, responsive layouts and clean console. Actual gRPC request/response and Redis publication/consumption are present in service logs. See docs/verification.md. Remaining workflows stay in ticket04.
