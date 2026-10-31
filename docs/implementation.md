# Initial application increment

The first runnable increment implements the core counter workflow. The complete target scope remains in [system-design.md](system-design.md); features listed as pending below are not implemented endpoints or screens.

## Implemented

- [Source-backed soil metadata and product advice](soil-advice.md): nine factors, Admin review/invalidation, staff filters, explainable top-three gRPC matches and sale-draft creation.

- Layered .NET 10 REST API, Application services/contracts, Infrastructure repositories and Domain entities.
- PostgreSQL/Npgsql EF model for all 18 design tables, initial migrations and an [idempotent exported SQL script](database/ef-migrations.sql). Tables for later workflows exist without implying those workflows are implemented.
- JWT login, bootstrap Admin from configuration, password hashing, Admin/Employee authorization and active-account/role/version validation on every authenticated request. No public registration. React keeps its token in memory; reloading signs out.
- Product CRUD in the API, product create/edit UI, supplier create/list, SKU create/read/update API and packaging creation UI. AutoMapper maps entities to explicit read DTOs. Stock, roles, totals and status are absent from write DTOs; unexpected JSON fields are rejected.
- Product search, active filter, allowlisted sort and pagination; inventory search/low-stock filter/pagination; paged documents and stock movements.
- Product-type selection and catalogue/inventory filters for six packaged-mix groups, plus Unclassified for unknown data. A repeatable explicit demo seed populates catalogue and trading records; see [demo data](demo-data.md).
- Receipt/sale draft creation and confirmation. Receipts record actual whole-VND costs. Sales freeze catalogue prices at confirmation. Drafts do not reserve bags.
- Independent HTTP/2 gRPC host for deterministic totals, invoked by API before acquiring stock locks, with a three-second deadline.
- PostgreSQL document locks, ordered SKU row locks, guarded stock updates and a single transaction containing document/line snapshots, movements and outbox records. Repeating a confirmed action key has one effect; other attempts to confirm the same document conflict.
- API Redis Streams producer from its durable outbox; Worker consumer group with pending-entry reclaim, post-commit ACK and current-state low-stock reconciliation every minute. Duplicate delivery updates one active suggestion rather than creating duplicates.
- React inventory, catalogue, receipts, sales, movement history and read-only replenishment inbox, using plain CSS. Six-service Docker Compose deployment.

## Run locally

Prerequisites: Docker Desktop in Linux-container mode and PowerShell 7. Local .NET/Node installations are not required for Compose.

From the repository root:

```powershell
./scripts/setup.ps1
docker compose up -d --build
```

Open <http://localhost:8080>. Read `BOOTSTRAP_USERNAME` and `BOOTSTRAP_PASSWORD` from the ignored root `.env` locally to sign in; setup does not print secrets. Existing `.env` files are preserved. Changing `.env` bootstrap values after the first database initialization does not reset an existing account. Keep the original local credentials with the database volume.

Only Web binds a host port, on loopback. PostgreSQL, Redis, API and gRPC communicate inside the Compose network. Change `WEB_PORT` in `.env` if needed. This is a local assignment setup; it does not configure an internet-facing HTTPS deployment.

```powershell
docker compose ps
docker compose logs -f api worker calculation
docker compose down
```

`down` preserves the database and Redis volumes. Do not delete volumes to troubleshoot an ordinary error. API applies EF migrations before accepting requests; Worker retries while the schema is starting.

OpenAPI JSON is available at <http://localhost:8080/api/openapi/v1.json>. Swagger UI is registered on the API host; only the JSON contract is proxied through the default Web configuration. Send JWT bearer tokens for authenticated API requests.

## First business walkthrough

1. In Catalogue, create a product with optional brand/source information; leave unknown fields blank.
2. Create a supplier and a packaging/SKU variant, for example 5 kg at 55,000 VND. Initial stock is zero.
3. In Purchase receipts, create a draft for six bags at 30,000 VND cost, review it, then confirm.
4. In Sales, create a two-bag draft and confirm. The frozen sale total is 110,000 VND and current stock is four bags.
5. Inventory shows low stock at the default threshold of five. Replenishment shows a suggestion for 26 bags toward the default target of 30 after the Worker handles the event.
6. Stock movements show both changes. Creating a draft alone has no stock effect.

The automated browser smoke creates clearly labelled demo catalogue records and documents using this walkthrough. They remain in the local database as reviewable demo data; the script does not delete business history.

## Development and verification

Install .NET SDK 10.0.402 (or a compatible 10.0.4xx patch according to `global.json`) and Node 22.12+ for native development. This workspace also has an ignored `.tools/dotnet` SDK; it is not part of the repository.

```powershell
dotnet tool restore
dotnet build SoilShop.sln
./scripts/test.ps1
cd src/soilshop-web
npm ci
npm run build
```

The PowerShell test runner creates a uniquely named disposable PostgreSQL 16 container, supplies a random test password and removes only that container in `finally`. It never runs integration tests against the shop volume. Tests replace the calculation network boundary with a deterministic implementation; the Compose/browser walkthrough tests the actual independent gRPC host.

For browser checks, install Python `playwright` in your preferred virtual environment and Google Chrome, then run `python scripts/browser_smoke.py` with Compose running. Results/screenshots are written under ignored `test-results/`. The script reads local bootstrap credentials without printing them and requires them to match the initialized database. It verifies stock, documents, low-stock processing, four viewport sizes and browser errors.

Export reviewed migrations after a database-model change:

```powershell
dotnet ef migrations script --idempotent --project src/SoilShop.Infrastructure --startup-project src/SoilShop.Api --output docs/database/ef-migrations.sql
```

Use EF migrations for the application. `docs/database/schema.sql` remains the separately verified design baseline; do not apply both scripts to the same database. The EF schema adds its migration-history table and conventional FK indexes. Case-insensitive expression indexes are added explicitly by the second migration. No business seed password is included in migrations.

## Package configuration

Current application pins .NET/EF 10, Npgsql provider 10.0.3, AutoMapper 16.2.0, gRPC 2.84.0 and StackExchange.Redis 2.10.1. Frontend dependency resolution is recorded in `package-lock.json` and installed with `npm ci`.

AutoMapper license configuration accepts `AutoMapper__LicenseKey` / the Compose `AUTOMAPPER_LICENSE_KEY` environment value. Review the [official license configuration](https://docs.automapper.io/en/stable/License-configuration.html) for eligibility and use. Missing-key messages remain visible; no license key or purchase is supplied by this repository.

Whole-VND unit prices/costs are capped at 1,000,000,000,000, bag quantities at 1,000,000 per line and documents at 100 distinct SKU lines. Document totals are capped at 9,000,000,000,000,000 VND so integer totals remain exact in browser numbers. Package amounts support at most two decimal places. PostgreSQL uses `numeric(18,0)` for money and rejects negative stock.

## Pending approved work

- Full supplier/account management and SKU price editing in the UI.
- Draft editing/deletion and whole-document Admin cancellation/reversal.
- Physical count capture, stale-count handling, Admin approval and direct adjustments.
- Replenishment-to-receipt conversion.
- Daily/manual summaries and daily/manual PostgreSQL backups retaining seven successful archives. The Worker currently processes inventory events only; no automatic backup runs yet.
- Broader broker interruption/recovery and job failure tests, complete demo seed/presentation and final assignment acceptance review.

Selection forms explicitly show at most the first 100 SKUs/suppliers; packaging creation uses products on the current catalogue page. A searchable selector is a later usability improvement. Document filters and advanced list controls remain pending. Track subsequent slices in `.scratch/application/issues/04-remaining-workflows.md`.
