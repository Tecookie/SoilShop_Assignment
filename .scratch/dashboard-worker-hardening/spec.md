# Dashboard and worker message handling

Status: Complete

Add live, read-only Vietnam-date sales and receipt statistics grouped by day or
Monday-to-Sunday week. Show cancelled-document treatment, current low stock and
the distinction between purchase cost and profit. Keep existing stock/account/
supplier/count and paginated selector workflows intact.

Dispatch Redis events by kind; handle failures independently, cap attempts using
Redis delivery counts and atomically quarantine exhausted messages while allowing
other messages through. Preserve the durable PostgreSQL outbox and queued jobs.
Expose validated, bounded Worker settings as unprefixed Compose environment keys.

## Comments

- User requested this set on 2026-11-11 and delegated configuration naming judgment.
- Verification must use isolated PostgreSQL/Redis and cover bad and valid messages,
  daily/weekly totals, invalid settings, existing authorization and responsive UI.
- Verified 47 PostgreSQL-backed tests, 6 isolated dashboard/Redis checks and
  responsive dashboard screenshots. See `docs/dashboard-worker-checks.json`.
