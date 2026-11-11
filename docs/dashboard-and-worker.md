# Dashboard and Worker operations

## Dashboard

Open **Dashboard** as Admin or Employee. Choose a Vietnam-time date range of at
most 366 days and group results by **day** or **week** (Monday–Sunday, clipped at
the selected range). The cards and period table show confirmed sales, bags sold,
confirmed purchase receipts, bags received and their respective totals. The
low-stock figure is current at query time. Zero-activity periods are included.

Statistics use the document's confirmation time and **current** status: a later
cancellation removes that document when the dashboard is reloaded. Purchase
amount is what the shop recorded on receipts, not cost of goods sold, so the
difference from sales is not accounting profit. A repeatable-read transaction
provides a consistent dashboard snapshot. Saved daily report snapshots remain a
separate workflow described in [operations workflows](operations-workflows.md).

`GET /api/dashboard?from=2026-11-01&to=2026-11-07&groupBy=day` requires JWT;
`groupBy` accepts `day` or `week`. Invalid, future or overlong ranges return 400.

## Worker configuration

Optional root keys have **no Worker prefix**. Set them in the local `.env`, then
recreate the Worker with `docker compose up -d worker`. The API and calculation
service do not consume these keys. Invalid values stop Worker startup rather than
silently using an unsafe interval.

| Key | Default | Accepted | Meaning |
| --- | ---: | ---: | --- |
| `BatchSize` | 25 | 1–100 | Messages read/claimed per poll |
| `PollSeconds` | 2 | 1–60 | Delay between message polls |
| `ReclaimSeconds` | 60 | 1–3600 | Minimum pending idle time before retry |
| `ReconcileSeconds` | 60 | 5–3600 | Current-stock reconciliation interval |
| `MaxRetries` | 3 | 0–20 | Retries **after** the first failed delivery |
| `JobPollSeconds` | 10 | 1–300 | Interval for independent summary and backup loops |

For example, `MaxRetries=3` allows four deliveries total. These settings affect
retry speed and workload; they do not change the database stock transaction or
the retention policy of seven successful backups.

## Event dispatch, retries and quarantine

The `soilshop:events` stream and `soilshop-worker` consumer group keep their
existing names so previously queued events remain compatible. The Worker handles
each message separately:

1. Validate the event ID and JSON payload, then dispatch `InventoryChanged`,
   `SummaryRequested` or `BackupRequested` by kind.
2. Inventory events trigger idempotent reconciliation and are acknowledged after
   it commits. Summary/backup notifications are acknowledged once validated;
   their independently polled PostgreSQL rows are the durable job authority.
3. A failed message stays pending and is reclaimed after `ReclaimSeconds`.
   Healthy messages in the same or later batches continue processing.
4. After the configured retries, an atomic Redis script copies the source ID,
   event kind, payload, failure type and delivery count to
   `soilshop:events:dead-letter`, then acknowledges the original. If quarantine
   fails, the original remains pending for another attempt.

Inspect quarantine from a trusted operator terminal:

```powershell
docker compose exec -T redis redis-cli XRANGE soilshop:events:dead-letter - + COUNT 20
docker compose exec -T redis redis-cli XPENDING soilshop:events soilshop-worker
docker compose logs --tail 100 worker
```

Quarantined events do not automatically replay. Investigate and correct the
underlying issue before a deliberate replay; repeated inventory events reconcile
current state, but other kinds must retain their own durable job identity. Do not
delete or trim pending events. The dead-letter stream has no automatic retention
policy in this assignment and should be monitored in a longer-running deployment.

See [verification](verification.md) for isolated malformed/unknown-message tests
and [assignment traceability](assignment-traceability.md) for demonstration.
