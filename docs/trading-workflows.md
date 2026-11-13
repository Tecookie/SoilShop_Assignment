# Advice, purchase receipts and counter sales

**Scope:** approved [system design](system-design.md), sections **5.2–5.4**.

| Section | What staff can do | Delivery |
| --- | --- | --- |
| **5.2 · Product advice** | Find matching SKUs, inspect reasons/source, create a sale Draft | Existing implementation retained and regression-tested; [detailed guide](soil-advice.md) |
| **5.3 · Purchase receipts** | Create/edit/delete Drafts, review costs, confirm receipt, Admin cancel | Completed in this branch |
| **5.4 · Counter sales** | Create/edit/delete Drafts, review current prices, confirm sale, Admin cancel | Completed in this branch |

**Read next:** [Staff workflow](#1-staff-workflow) · [API contract](#2-api-contract) · [Transaction rules](#3-transactions-and-immutable-history) · [Code map](#4-implementation-map) · [Verification](#5-verification-and-demo)

> **Stock changes only when a document is confirmed or cancelled.** Advice, quotes and Drafts never reserve or change bags.

## 1. Staff workflow

### 5.2 · Explainable product advice

1. Admin records product metadata and a source, then explicitly reviews the saved version.
2. Employee selects needs in **Tìm đất phù hợp** and reads up to three matches.
3. Results show matching reasons, missing information and the reviewed source.
4. **Tạo phiếu bán nháp · 1 bao** opens a sale Draft. Staff then follow the sale flow below.

The existing `advice-v2` rules, nine metadata factors and review invalidation are documented in [soil-advice.md](soil-advice.md).

### 5.3 · Purchase receipts

| Step | Staff action | Result |
| --- | --- | --- |
| 1 | **New receipt** → choose active Supplier, SKUs and quantities | A new receipt form |
| 2 | Enter actual whole-VND costs, or leave unknown costs blank → **Save draft** | Draft saved; stock unchanged |
| 3 | **Edit draft** to correct supplier/lines; **Delete** to remove an unused Draft | Version checked; stock unchanged |
| 4 | Fill every cost → **Review total** | gRPC calculates the receipt total |
| 5 | Review quantities/costs → **Confirm receipt** | Stock increases; Purchase movements recorded |

### 5.4 · Counter sales

| Step | Staff action | Result |
| --- | --- | --- |
| 1 | **New sale** → choose active SKUs and bag quantities | No client-controlled prices |
| 2 | **Save draft**; edit/delete if needed | Draft saved; no stock reservation |
| 3 | **Review total** | Current catalogue prices and gRPC total shown |
| 4 | Review → **Confirm sale** | Stock decreases; prices/labels/total are frozen |
| If prices change | Read the `409` message and refreshed quote, then confirm again | New prices require explicit review |

### Admin cancellation

Select **Cancel document**, enter a reason and choose **Confirm cancellation**.

| Document | Stock effect | Condition |
| --- | --- | --- |
| Confirmed sale | Restore all original quantities | Whole-document cancellation only |
| Confirmed receipt | Deduct all original quantities | Every SKU must have sufficient current stock |

Original lines, frozen prices, total and confirmation actor/time remain readable. Cancellation adds its own reason, actor, timestamp and reverse movements.

### Document lifecycle

| State | Edit/delete | Quote/confirm | Cancel |
| --- | --- | --- | --- |
| **Draft** | Staff | Staff; receipt needs all costs | Unavailable |
| **Confirmed** | Unavailable | Successful same-key retry only | Admin |
| **Cancelled** | Unavailable | Successful earlier confirmation replay returns current state | Successful same-key cancellation retry only |

On narrow screens, tables scroll horizontally inside their panel. Errors preserve form values; pending writes disable relevant actions. Action keys survive uncertain network retries, while known state/price conflicts require review. Reloading clears the in-memory JWT, following the existing authentication design.

## 2. API contract

### Routes and permissions

The following routes exist for both `/api/purchase-receipts` and `/api/sales`:

| Method / suffix | Purpose | Authorization |
|---|---|---|
| GET collection, GET `/{id}` | Paged list and document detail | Authenticated staff |
| POST collection | Create Draft | Authenticated staff |
| PUT `/{id}` | Replace Draft lines/supplier | Authenticated staff |
| DELETE `/{id}?expectedVersion=…` | Delete current Draft | Authenticated staff |
| GET `/{id}/quote` | Current prices/costs and gRPC total | Authenticated staff |
| POST `/{id}/confirm` | Confirm once, atomically changing stock | Authenticated staff |
| POST `/{id}/cancel` | Reverse whole Confirmed document | Admin only |

### Input rules

| Field / rule | Receipt | Sale |
| --- | --- | --- |
| `lines` | 1–100 input lines | 1–100 input lines |
| `skuId`, `quantity` | Active SKU; 1–1,000,000 bags | Active SKU; 1–1,000,000 bags |
| `supplierId` | Required, active Supplier | Omit |
| `unitCostVnd` | Positive whole VND; nullable in Draft | Omit; price comes from catalogue |
| Duplicate SKU | Merge only when costs agree, including all-unknown costs | Merge quantities |
| `expectedVersion` | Required for update/delete/confirm/cancel | Same |

Merged quantities cannot exceed 1,000,000. Conflicting receipt costs are rejected rather than averaged.

### Example: quote and confirmation

**Quote response** — `GET /api/sales/{id}/quote`:

```json
{
  "version": 1,
  "totalVnd": 100000,
  "lines": [{ "skuId": 1, "quantity": 2, "unitPriceVnd": 50000 }]
}
```

**Confirmation request** — `POST /api/sales/{id}/confirm`:

```json
{
  "actionKey": "a5d31a89-d636-4413-b94c-8c3420c796b8",
  "expectedVersion": 1,
  "expectedTotalVnd": 100000
}
```

`expectedTotalVnd` is optional for compatibility with existing service callers,
seed scripts and the original confirmation contract. React always supplies it
after review. It is a precondition, never an authoritative assignment to totals.
Authoritative calculation and per-line price checks apply even when it is omitted.

### Example: cancellation

`POST /api/sales/{id}/cancel` — Admin only:

```json
{
  "actionKey": "f971dd83-cfb6-4500-a451-063d03210ad7",
  "expectedVersion": 2,
  "reason": "Customer cancelled the entire sale"
}
```

Use a new action key and the Confirmed version. The trimmed reason must contain 1–500 characters. Read DTOs include created/confirmed/cancelled actors and timestamps, plus the cancellation reason. Persisted action keys are not exposed as mutable document properties.

### Error responses

| HTTP status | Meaning | Staff response |
| --- | --- | --- |
| `400` | Invalid input or missing receipt cost | Correct the form |
| `401` / `403` | Missing login / insufficient role | Sign in or use the permitted role |
| `404` | Document not found | Refresh the document list |
| `409` | Stale version/state, conflicting action reuse or insufficient stock | Reload and review before retrying |
| `503` | gRPC calculation unavailable | Retry later; stock is unchanged |

Errors use the existing **ProblemDetails** format. Price-related `409` responses include a `quote` extension for explicit review. If gRPC cannot calculate that refreshed quote, the API returns `503` and still leaves stock unchanged.

## 3. Transactions and immutable history

### Quote and confirmation

- A quote reads document version, lines and prices in one RepeatableRead snapshot,
  completes that read transaction, then calls gRPC before acquiring stock locks.
- Confirmation locks the document, checks version, and locks SKUs in ascending ID
  order. Every quantity and unit price must match the values sent to gRPC. Comparing
  only the combined total is insufficient: two prices can change in opposite directions.
- Draft updates and deletions take the same document lock, preventing a concurrent
  edit from changing lines beneath confirmation. Confirmed/Cancelled history cannot
  be edited or physically deleted through these endpoints.
### Cancellation and retry

- Cancellation locks the document and SKUs in the same order. Guarded SQL stock
  changes, reverse `PurchaseCancel`/`SaleCancel` movements, document audit fields
  and `InventoryChanged` outbox messages commit together. Failure on any line rolls
  back all lines. Reversal allows deactivated products/SKUs with legitimate history.
- Same successful action key and matching preconditions return the recorded current
  document without another stock effect. Changed confirmation values or cancellation
  reason/version conflict. A confirmation replay after cancellation returns the
  current Cancelled document and never reapplies the sale/receipt.
- Deleting a receipt Draft linked to a replenishment suggestion clears its FK,
  reopens the suggestion and increments its version in the same transaction.
  Suggestion-to-receipt conversion is described in [operations workflows](operations-workflows.md).

### Existing database structure

No new table or migration is required. Existing cancellation columns, document
constraints, unique action/ledger indexes and outbox are reused. The initial schema
and C4/ERD boundaries are unchanged. No payments, partial returns, repacking,
discounts or accounting valuation are added.

## 4. Implementation map

| Layer | Files |
|---|---|
| API | `src/SoilShop.Api/Controllers/Trading/TradingController.cs`, ProblemDetails in `Middleware/ShopExceptionHandler.cs` |
| Application | `SoilShop.Application/Trading/TradeContracts.cs` and `TradingService.cs` |
| Infrastructure | `SoilShop.Infrastructure/Trading/TradingRepository.cs`, `.Drafts.cs`, `.Cancellation.cs` |
| Web | `features/trading/TradingPage.tsx`, its `components/` and `types.ts`, `shared/api/client.ts`; role passed by `app/App.tsx` |
| Existing advice | `Application/Advice/ProductAdviceService.cs`, `Infrastructure/Advice/AdviceCandidateRepository.cs`, gRPC `Advice/AdviceRules.cs`, React `features/advice/` |
| Tests | `TradingWorkflowTests.cs`, existing trading/advice/rule tests |

## 5. Verification and demo

### Commands

Run `./scripts/test.ps1` for isolated PostgreSQL tests, then build the frontend with
`npm ci` and `npm run build` from `src/soilshop-web`. With Compose and local `.env`
credentials, use Python with Playwright/Chrome to run:

```powershell
python scripts/browser_smoke.py
python scripts/browser_trading_check.py
python scripts/browser_advice_check.py
```

### Demonstration checkpoints

| Checkpoint | Expected result |
| --- | --- |
| Save/edit receipt Draft with an initially unknown cost | Stock remains zero |
| Complete costs and confirm six bags | Stock becomes six |
| Quote two-bag sale, then change catalogue price | Confirmation returns `409` and a refreshed quote |
| Review and confirm sale | Stock becomes four; accepted prices are frozen |
| Try cancelling six-bag receipt | `409`; stock stays four |
| Cancel sale, then cancel receipt | Stock becomes six, then zero |
| Delete a separate Draft | Draft disappears; stock unchanged |
| Inspect ledger and layouts | Four movement kinds; 390px/1440px checks |

The browser script uses labelled fictional fixtures and preserves their history. It does not perform a destructive cleanup.

See the dated results in [verification.md](verification.md),
[trading checks](trading-checks.json), [desktop](screenshots/trading-desktop.png)
and [mobile](screenshots/trading-mobile.png). The advice browser fixture's review
is cleared after testing so simulated metadata is not left eligible for advice.
