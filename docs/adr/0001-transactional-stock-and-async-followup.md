# Keep stock authoritative in PostgreSQL

Two staff members can sell the final bag concurrently, so receipt/sale confirmation, cancellation, count approval, ledger and outbox changes commit in one PostgreSQL transaction with row locking and conditional stock updates. Redis is used only for post-commit alerts/jobs; an API-hosted outbox relay publishes them and a Worker consumes them. This adds one durable-intent table but avoids lost follow-up work after a successful DB commit and avoids a distributed stock-reservation subsystem.
