# Counter workflow demonstration

This guide covers the **currently implemented application**, including real service communication. Plan approximately 12–15 minutes for the main flow and an additional 5–10 minutes for selected technical/failure exercises. Timings are presentation estimates. All commands run in PowerShell 7 from the repository root.

## 1. Preparation

```powershell
docker compose up -d
docker compose ps
```

All six services should be running: `web`, `api`, `calculation`, `worker`, `postgres`, `redis`; database/broker health checks should be healthy. Open <http://localhost:8080>.

Use Admin (`BOOTSTRAP_*`) and Employee (`DEMO_EMPLOYEE_*`) credentials from ignored `.env`. Keep credentials and login responses off the presentation screen. Reloading a browser page clears the in-memory session, so sign in again if needed. Two tabs can sign in independently.

If sample data is missing, run `./scripts/seed-demo.ps1`. It preserves existing data and does not replenish already-used seed stock on subsequent runs. Use a **fresh SKU** for reproducible arithmetic. Never clear the database/volumes for the demonstration.

Use run code `DEMO-FLOW-01`; change `01` to `02`, etc. for another rehearsal. Use the same code in the SQL examples below.

Prepare two additional terminals:

```powershell
# Terminal A: show actual HTTP/2 calculation calls.
docker compose logs -f --tail 0 calculation
```

```powershell
# Terminal B: show the API producer and Worker consumer.
docker compose logs -f --tail 0 api worker |
    Select-String 'Published outbox|Consumed inventory event|will retry'
```

`Ctrl+C` stops log following only. These commands do not stop containers.

## 2. Main business flow

| Step | Action | Expected result | What it demonstrates |
|---|---|---|---|
| 1 | Sign in as Admin. Open Catalogue. Show the product groups and existing Vietnamese product names. | Catalogue is available to Admin. | React → REST API; JWT authentication and Admin authorization. |
| 2 | Create Supplier code `DEMO-FLOW-01-SUP`, name `Nhà cung cấp mẫu 01`. Create Product code `DEMO-FLOW-01`, name `Đất trồng đa dụng trình diễn 01`, type `Đất trồng đa dụng`. Leave unknown optional metadata blank. | Supplier and Product appear. | Validated master-data creation, AutoMapper read DTOs, EF Core/Npgsql persistence. |
| 3 | Create packaging/SKU code `DEMO-FLOW-01-5KG`, select that Product, amount `5`, unit `kg`, sale price `55000`, threshold `5`, target `30`, preferred Supplier created above. | Inventory has **0 bags** at **55,000 VND/bag**. | Product/SKU separation. A catalogue form cannot assign opening stock. |
| 4 | Sign out; sign in as Employee. | Inventory/receipts/sales remain available; Catalogue navigation is absent. | Role-specific UI. Server-side 403 enforcement is separately covered by integration tests; hiding a button alone does not prove it. |
| 5 | Purchase receipts → New receipt. Select the Supplier and SKU, enter **6 bags**, unit cost **30000**. Save draft. Check Inventory, then return to that receipt. | Draft exists and stock remains **0**. | Drafts have no stock effect. Remember the reference to avoid confirming another rehearsal's draft. |
| 6 | Confirm that receipt. | Receipt total **180,000 VND**; stock **6**; a Purchase movement **+6 / balance 6**. Terminal A shows a calculation request. | API calls the independent gRPC host before the transaction. PostgreSQL commits document, stock, ledger and outbox together. |
| 7 | Sales → New sale. Select the same SKU and **2 bags**. Save draft and check stock before confirming. | Stock remains **6**. | Sales drafts do not reserve or deduct bags. Sale price is controlled by the catalogue/service. |
| 8 | Confirm that sale. | Frozen total **110,000 VND**; stock **4**; a Sale movement **−2 / balance 4**. Terminal A logs another actual gRPC calculation. | Frozen sale prices, gRPC totals and transactional stock deduction. |
| 9 | Inventory: search by SKU code, select `Đất trồng đa dụng`, enable Low stock only. Then open Stock movements. | The SKU is Low stock at **4**; history contains its **+6** and **−2** movements. | Search/type/low-stock filters, correct accounting and persisted history. |
| 10 | Observe Terminal B. Open Replenishment and Refresh after processing. Locate the demo SKU's numeric ID. | One active suggestion with observed quantity **4**, suggested quantity **26 = 30 − 4**. | API producer → Redis Streams → Worker consumer → PostgreSQL. Confirm delivery using the diagnostics below. |

The newly created zero-stock SKU may already have an initial suggestion. Receiving six bags can resolve it; selling two can create a new active suggestion. Historical resolved suggestions may exist. The invariant is **one active suggestion per SKU**, not one suggestion ever.

The main-flow checkpoint is: **received 6, sold 2, current 4, suggested replenishment 26**. Refresh read screens if they were open before a transaction; the UI does not push real-time updates automatically.

## 3. Show the technology evidence

### Actual gRPC, rather than a local method call

Terminal A should contain:

```text
HTTP/2 POST ... /soilshop.Calculation/CalculateTotals
Calculated 1 lines using totals-v1
... 200 ... application/grpc
```

The independent `calculation` container receives quantity/unit-price lines and returns a whole-VND total. It does not access PostgreSQL. The current service calculates totals; product recommendation ranking remains pending.

### Transactional stock and EF migrations

```powershell
docker compose exec -T postgres psql -U soilshop -d soilshop -c "SELECT id,code,stock_qty,stock_version,sale_price_vnd FROM skus WHERE code='DEMO-FLOW-01-5KG';"

docker compose exec -T postgres psql -U soilshop -d soilshop -c "SELECT m.kind,m.delta_qty,m.balance_after,m.stock_version_after FROM stock_movements m JOIN skus s ON s.id=m.sku_id WHERE s.code='DEMO-FLOW-01-5KG' ORDER BY m.id;"

docker compose exec -T postgres psql -U soilshop -d soilshop -c 'SELECT "MigrationId","ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'
```

At the main-flow checkpoint, expect stock `4`, stock version `2`, and movements `Purchase +6 → 6`, `Sale −2 → 4`. Querying the SKU also gives its ID for the Replenishment screen, which currently displays numeric SKU IDs.

### Outbox → Redis → Worker → ACK

```powershell
docker compose exec -T postgres psql -U soilshop -d soilshop -c "SELECT id,kind,payload,published_at FROM outbox_messages WHERE payload->>'skuId'=(SELECT id::text FROM skus WHERE code='DEMO-FLOW-01-5KG') ORDER BY created_at DESC LIMIT 5;"

docker compose exec -T redis redis-cli XREVRANGE soilshop:events + - COUNT 5
docker compose exec -T redis redis-cli XINFO GROUPS soilshop:events
docker compose exec -T redis redis-cli XPENDING soilshop:events soilshop-worker
```

Evidence to connect:

1. PostgreSQL outbox has kind `InventoryChanged`, payload containing this SKU ID, and eventually non-null `published_at`.
2. API logs `Published outbox <GUID> InventoryChanged`. The Redis entry's **field** `id` contains the same GUID.
3. Redis gives that entry its own **stream ID**, e.g. `...-0`. Worker logs `Consumed inventory event <stream ID>`.
4. Once delivery settles, the consumer group's `lag` and `pending` should both be zero. Pending zero alone is insufficient: messages not yet delivered can still contribute to lag.

`COUNT 5` shows the latest global messages. If other staff are active, increase the count and locate the payload/GUID for this SKU.

**Replenishment changing on screen alone is not proof of broker delivery.** Worker also reconciles current PostgreSQL stock at startup and every minute, even when Redis is unavailable. Use the entry IDs, producer/consumer logs and ACK state as the Redis evidence.

### REST, validation, mapping and authorization

- OpenAPI contract: <http://localhost:8080/api/openapi/v1.json>.
- Anonymous read should return 401:

```powershell
(Invoke-WebRequest http://localhost:8080/api/skus -SkipHttpErrorCheck).StatusCode
```

- An authenticated Employee attempting catalogue writes receives 403. This is tested by `scripts/test.ps1`.
- Product API supports `search`, `productType`, `active`, allowlisted `sort`, `page`, `pageSize`. Example authenticated path: `/api/products?productType=GeneralPurpose&sort=code&page=1&pageSize=2`.
- A SKU write DTO omits trusted `stockQty`, `stockVersion` and document totals/status. Adding an unknown JSON property such as `stockQty: 999` to a create request returns 400; the integration suite checks this. AutoMapper maps entity → read DTO, while Services control business state.
- Product create/edit is available in UI. Product delete and SKU price update are currently API operations; do not claim every CRUD action already has a screen. Referenced products cannot be deleted.

## 4. Last-bag conflict and idempotency

After the main checkpoint at four bags:

1. Confirm another sale of **3 bags**, total **165,000 VND**. Stock becomes **1**.
2. Open two independent tabs and sign in separately, e.g. Admin and Employee.
3. Each creates its own one-bag sale draft for the demo SKU. Both drafts are valid and stock remains one.
4. Try confirming both closely together. Expect **one success at 55,000 VND**, the other **409 Conflict / insufficient stock**. The losing document remains Draft; stock is zero.
5. Ledger for the entire run is **+6, −2, −3, −1**, with no second last-bag deduction.

Two manual clicks demonstrate conflicting attempts, but cannot guarantee their HTTP requests overlap. For repeatable concurrent requests and retry checks, run:

```powershell
./scripts/test.ps1
```

Point to `Last_bag_has_one_winner_and_duplicate_action_has_one_effect`: it sends two confirmations using `Task.WhenAll`, asserts one success/one conflict, retries the winning action key and asserts no second effect. The suite also verifies all-or-nothing rollback for multi-line shortages and frozen historical prices.

These HTTP integration tests use real isolated PostgreSQL but replace the calculation network boundary. **The main Compose flow above supplies the separate evidence of real gRPC communication.** Test containers are separate from the shop volume.

Current UI removes Confirm after success. Refreshing the page is not an idempotency test; it only issues reads. Action-key replay is verified by the API integration test.

## 5. Optional dependency-failure exercises

Choose one or two exercises; record the current stock before each. They are additional live tests with expected outcomes based on the implementation, not claims that this documentation-writing session triggered outages. Restore the stopped service when done. Use an active SKU with enough stock; the race exercise above leaves its SKU empty.

### A. gRPC unavailable: confirmation must not change stock

1. Create a sale draft for one available bag and note current stock `N`.
2. In a terminal:

```powershell
docker compose stop calculation
```

3. Confirm the draft. Expect HTTP **503** and the calculation-unavailable message, normally within the configured three-second RPC deadline. Draft and stock `N` stay unchanged; no movement/outbox from this failed confirmation.
4. Restore:

```powershell
docker compose start calculation
```

5. Wait for the host to start, then retry the same draft. Expect one confirmation, stock `N−1` and one movement. The UI reuses its action key when retrying from the same page.

### B. Worker paused: asynchronous work can lag behind the sale

```powershell
docker compose stop worker
```

Confirm a one-bag sale while stock is positive. The API/gRPC/PostgreSQL transaction should still succeed. Redis can receive the event, but the consumer group's **lag increases** while Worker is stopped; `pending` may remain zero because delivery has not started. The suggestion may remain stale during this pause.

```powershell
docker compose exec -T redis redis-cli XINFO GROUPS soilshop:events
docker compose start worker
```

After restart, observe reconciliation, consumer logs, an updated suggestion and settled lag/pending. Startup reconciliation can update the suggestion before message consumption; inspect stream/ACK evidence as well.

### C. Redis unavailable: SQL outbox retains publication intent

```powershell
docker compose stop redis
```

Confirm another available one-bag sale. Expect PostgreSQL stock/history to commit while new outbox rows remain `published_at = NULL` and relay logs show retries. Worker may still update suggestions on its minute-based database reconciliation; do not use the suggestion as the outage indicator.

```powershell
docker compose start redis
```

Allow reconnect/processing time. Confirm previously unpublished rows receive `published_at`, Redis entries appear, Worker logs consumption and lag/pending settle. A repeated delivery must not create a second active suggestion. No SQL stock transaction depends on a Redis distributed lock.

## 6. Feature boundary and closing explanation

| Technology | Concrete evidence |
|---|---|
| React/Vite/TypeScript | Browser forms, filters, role navigation and conflict/error states. |
| ASP.NET Core REST/JWT | Login, protected CRUD/workflow endpoints, 400/401/403/409/503 behavior, OpenAPI. |
| Controller → Service → Repository | Trace Confirm controller → service validation/RPC → repository's atomic SQL transaction. |
| AutoMapper | Explicit entity-to-read-DTO profiles; unexpected write fields rejected. |
| EF Core/Npgsql/PostgreSQL | Applied migrations, retained records, stock version, ledger and transaction rollback. |
| Independent gRPC host | Actual HTTP/2 CalculateTotals requests in `calculation` container. |
| Redis Streams producer | API's outbox relay publishes XADD entries. |
| .NET Worker consumer | XREADGROUP delivery, business reconciliation, XACK and pending-entry reclaim. |
| Docker Compose | Six running services and persistent PostgreSQL/Redis volumes; only Web exposed on loopback. |

A suitable closing statement is: “Staff confirm a sale through REST; the API obtains a total from gRPC, then commits stock and audit records in PostgreSQL. Redis carries post-commit work to Worker, so replenishment is asynchronous while stock correctness stays transactional.”

Recommendations, stocktaking/adjustments, cancellation, draft editing/deletion, replenishment conversion, reports and daily/manual backups remain pending. Do not present them as implemented. The independent gRPC service currently provides totals, and replenishment is a **read-only suggestion inbox**. Full status is in [implementation.md](implementation.md).

Automated rehearsal: [browser_smoke.py](../scripts/browser_smoke.py) creates its own labelled demonstration records and exercises actual Compose services. [browser_seed_check.py](../scripts/browser_seed_check.py) performs read-only seed/filter/role checks. [test.ps1](../scripts/test.ps1) covers repeatable API/database edge cases.
