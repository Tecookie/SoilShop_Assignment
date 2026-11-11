# Stock, replenishment, reports and backups

This guide describes the current internal-shop workflows. PostgreSQL remains the
authority for inventory. A Draft, count submission, suggestion or queued job does
not itself change stock.

| Workflow | Employee | Admin | Stock effect |
| --- | --- | --- | --- |
| Capture a physical count | Submit | Submit or review | None until a current count is approved |
| Direct adjustment | View history | Record a reasoned adjustment | Immediate, with immutable movement |
| Low-stock suggestion | Convert to receipt Draft | Same | None until receipt confirmation |
| Daily summary | View and request refresh | Same | None; stored snapshot only |
| Database backup | No access | Queue and view runs | None; separate archive volume |
| Account, supplier and SKU rules | Use permitted catalogue | Maintain | Catalogue changes can request a stock recheck |

## 1. Physical count and direct adjustment

1. Open **Stock counts**, find the SKU, and capture the system quantity and stock
   version before counting the actual bags.
2. Enter the actual quantity and a reason. Submission returns `409` if a receipt,
   sale, cancellation or adjustment changed stock since capture; count again.
3. An Admin approves or rejects the Pending count with a note. Approval checks the
   captured stock and version again under a database lock. If either changed, the
   result is `RecountRequired`; no stock is overwritten.
4. A nonzero discrepancy creates an adjustment, stock movement and inventory
   event in the same transaction. An equal count is approved without a movement.

For known opening stock, damage or loss, an Admin may capture a SKU then enter a
signed nonzero adjustment and reason. The database rejects negative stock or
overflow. The client keeps its request key during a retry, so a repeated request
does not apply the same adjustment twice. Account role, approval status, quantity,
ledger balance and actor are validated or derived on the server.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/stock-counts?page=1&pageSize=20` | List counts and review outcomes |
| `POST` | `/api/stock-counts` | Submit captured and actual quantities |
| `POST` | `/api/stock-counts/{id}/review` | Admin approves or rejects a current count |
| `POST` | `/api/stock-adjustments` | Admin records a reasoned direct adjustment |

Example count submission:

```json
{
  "skuId": 1,
  "expectedQty": 8,
  "expectedStockVersion": 3,
  "actualQty": 6,
  "reason": "Physical count"
}
```

Review uses `{ "expectedVersion": 1, "approve": true, "note": "Checked" }`.
For direct adjustments provide `requestKey`, `skuId`, `deltaQty`,
`expectedStockVersion` and `reason`; `requestKey` is a client-generated UUID.

## 2. Replenishment to receipt

The Worker reconciles active low-stock SKUs against current PostgreSQL stock.
Open **Replenishment**, choose **Prepare receipt**, and select an active supplier.
The API locks the suggestion and SKU, creates one linked receipt Draft and uses
the current target minus stock as its quantity. A known reference cost is copied;
otherwise cost remains unknown. An identical repeat returns the linked Draft.

The receipt must still be reviewed and confirmed after goods arrive. The Worker
resolves a linked suggestion after confirmation or cancellation; deleting its
Draft reopens the suggestion. Stock history remains unchanged until confirmation.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/restock-suggestions` | Show active suggestions |
| `GET` | `/api/restock-suggestions/search` | Search and page through all active suggestions |
| `POST` | `/api/restock-suggestions/{id}/convert` | Create or return linked receipt Draft |

Conversion JSON: `{ "expectedVersion": 1, "supplierId": 2 }`.

## 3. Dated shop summaries

At or after **00:05 Vietnam time**, the Worker queues the previous business date
if missing. Staff may request a refresh for a selected date. Each report counts
sales confirmed within that Vietnam date and still Confirmed at generation time;
cancelled sales are excluded. The SKU snapshot shows stock **when the job runs**,
not reconstructed end-of-day inventory. A later cancellation can make an older
snapshot stale until a staff member refreshes it.

Report totals and SKU snapshots are replaced atomically in a repeatable-read
transaction. A failed refresh retains the previous snapshot and records failure.
Queued work survives a Worker restart.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/summaries` | Paged job status and totals |
| `GET` | `/api/summaries/{yyyy-MM-dd}` | Stored totals and SKU snapshot |
| `POST` | `/api/summaries/{yyyy-MM-dd}/refresh` | Queue a refresh; HTTP 202 is not completion |

## 4. Backups and restoration

At or after **00:15 Vietnam time** the Worker queues one daily run. Admin can
request a manual run with a UUID request key. The dedicated Worker loop uses
PostgreSQL 16 `pg_dump`, checks the archive with `pg_restore --list` and keeps
the seven most recent successful daily/manual archives combined. Failed runs do
not delete earlier good archives. Files live in the separate `backup-data` Docker
volume; see [backup and restore](backup-and-restore.md) for an isolated restore.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/backups` | Admin views runs, result and retention state |
| `POST` | `/api/backups` | Admin queues `{ "requestKey": "<UUID>" }` |

The archive is local to this Docker host. It does not provide offsite backup or
point-in-time recovery. A successful archive listing alone is not a restore test.

## 5. Administration and search

Admin can create/update/deactivate accounts and suppliers and edit sale price,
threshold, target and availability for an existing SKU. An account password is
hashed on write and never returned. Role/account/password changes invalidate
existing JWT sessions; at least one active Admin must remain. An Employee cannot
call account, backup or direct-adjustment endpoints.

SKU, product and supplier selectors search the server and page through matches.
Sales and receipt lists accept document-code, status, Vietnam creation date,
creator ID and (for receipts) supplier ID filters. The existing product, inventory
and movement lists retain their search and pagination behavior.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET`, `POST` | `/api/accounts` | Admin searches or creates accounts |
| `PUT` | `/api/accounts/{id}` | Admin updates with `expectedVersion` |
| `GET` | `/api/supplier-directory` | Search paginated suppliers; optional `active` |
| `PUT` | `/api/supplier-directory/{id}` | Admin updates with `expectedVersion` |
| `PUT` | `/api/skus/{id}` | Admin updates price/rules with `expectedVersion` |
| `GET` | `/api/sales` or `/api/purchase-receipts` | Optional `search`, `status`, `from`, `to`, `createdBy`, `supplierId` |

## 6. Verification

Run `./scripts/test.ps1` for isolated PostgreSQL integration tests and
`npm ci && npm run build` in `src/soilshop-web` for the frontend build. The
isolated browser/restore script is `scripts/browser_operations_check.py`; it
requires `SHOP_CHECK_PROJECT=soilshop-completion-check` and a separately created
Compose project of that exact name. It deliberately stops only that project's
Redis/Worker, tests backup retention/failure, and restores an archive into its
own temporary PostgreSQL container. Never point it at the live shop.
