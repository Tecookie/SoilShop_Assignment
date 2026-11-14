# Application implementation

The runnable application implements the internal-shop workflows listed below. The complete scope and constraints are in [system-design.md](system-design.md); final team acceptance is still required.

## Implemented

- [Source-backed soil metadata and product advice](soil-advice.md): nine factors, Admin review/invalidation, staff filters, explainable top-three gRPC matches and sale-draft creation.

- Layered .NET 9 REST API, Application services/contracts, Infrastructure repositories and Domain entities.
- PostgreSQL/Npgsql EF model for all 18 design tables, initial migrations and an [idempotent exported SQL script](database/ef-migrations.sql). Tables for later workflows exist without implying those workflows are implemented.
- JWT login, bootstrap Admin from configuration, password hashing, Admin/Employee authorization and active-account/role/version validation on every authenticated request. No public registration. React keeps its token in memory; reloading signs out.
- Product CRUD in the API, product create/edit UI, supplier create/list, SKU create/read/update API and packaging creation UI. AutoMapper maps entities to explicit read DTOs. Stock, roles, totals and status are absent from write DTOs; unexpected JSON fields are rejected.
- Product search, active filter, allowlisted sort and pagination; inventory search/low-stock filter/pagination; paged documents and stock movements.
- Product-type selection and catalogue/inventory filters for six packaged-mix groups, plus Unclassified for unknown data. A repeatable explicit demo seed populates catalogue and trading records; see [demo data](demo-data.md).
- Receipt/sale draft creation and confirmation. Receipts record actual whole-VND costs. Sales freeze catalogue prices at confirmation. Drafts do not reserve bags.
- [Complete receipt/sale workflow](trading-workflows.md): versioned Draft editing/deletion, duplicate-SKU merging, reviewed gRPC quotes, per-line price conflict checks and whole-document Admin cancellation with atomic reverse ledger/outbox. Audit actors/timestamps and frozen values remain readable.
- Independent HTTP/2 gRPC host for deterministic totals, invoked by API before acquiring stock locks, with a three-second deadline.
- PostgreSQL document locks, ordered SKU row locks, guarded stock updates and a single transaction containing document/line snapshots, movements and outbox records. Repeating a confirmed action key has one effect; other attempts to confirm the same document conflict.
- API Redis Streams producer from its durable outbox; Worker consumer group with pending-entry reclaim, post-commit ACK and current-state low-stock reconciliation every minute. Duplicate delivery updates one active suggestion rather than creating duplicates.
- [Stock and operations workflows](operations-workflows.md): count submission and version-checked Admin review, direct adjustments, idempotent replenishment-to-receipt conversion, searchable paginated selectors, document filters and Admin account/supplier/SKU management.
- [Dashboard and Worker](dashboard-and-worker.md): daily/weekly confirmed sales and purchases; unprefixed validated Worker configuration; per-message bounded retries and dead-letter isolation.
- Daily/refreshable Vietnam-date summaries with atomic stock snapshots. A separate Worker loop queues PostgreSQL 16 daily/manual backups, checks archives and retains seven successes in a dedicated volume.
- React inventory, catalogue, receipts, sales, count approval, replenishment, summary and administration screens, using plain CSS. Six-service Docker Compose deployment.

## Run locally

Prerequisites: Docker Desktop in Linux-container mode and PowerShell 7. Local .NET/Node installations are not required for Compose.

From the repository root:

```powershell
./scripts/setup.ps1
docker compose up -d --build
```

Open <http://localhost:8080>. Read `BOOTSTRAP_USERNAME` and `BOOTSTRAP_PASSWORD` from the ignored root `.env` locally to sign in; setup does not print secrets. Existing `.env` files are preserved. Changing `.env` bootstrap values after the first database initialization does not reset an existing account. Keep the original local credentials with the database volume.

Only Web binds a host port, on loopback. PostgreSQL, Redis, API and gRPC communicate inside the Compose network. Change `WEB_PORT` in `.env` if needed. This is a local assignment setup; it does not configure an internet-facing HTTPS deployment.

If Docker Compose on Windows normalizes a Unicode/OneDrive checkout path and
reports that its build context does not exist, use an ASCII-path junction to
the same checkout and pass its path explicitly. Do not change the project's
build context or move existing database volumes to work around this host issue:

```powershell
$buildPath = Join-Path $env:TEMP 'soilshop-assignment-build'
# Create once, only if this path does not already exist.
New-Item -ItemType Junction -Path $buildPath -Target (Get-Location).Path
docker compose --project-directory $buildPath -f "$buildPath/compose.yaml" up -d --build
```

```powershell
docker compose ps
docker compose logs -f api worker calculation
docker compose down
```

`down` preserves the database and Redis volumes. Do not delete volumes to troubleshoot an ordinary error. API applies EF migrations before accepting requests; Worker retries while the schema is starting.

OpenAPI JSON is available at <http://localhost:8080/api/openapi/v1.json>. Swagger UI is registered on the API host; only the JSON contract is proxied through the default Web configuration. Send JWT bearer tokens for authenticated API requests.

## Debug the API in Visual Studio

With Docker Desktop running, execute from the repository root:

```powershell
.\scripts\setup-debug.ps1
```

This opt-in setup starts only PostgreSQL, Redis and Calculation using
`compose.yaml` plus `compose.debug.yaml`. It exposes those dependencies on
loopback ports 15432, 16379 and 18092, and copies connection settings, JWT key
and bootstrap credentials from the ignored `.env` into API User Secrets.
The normal Compose deployment remains private unless the debug override is selected.
Existing database volumes and credentials are preserved.

Select `SoilShop.Api` as the Visual Studio startup project and use its
Development launch profile, then press F5. The API initializes/migrates the
configured local database on startup. Its HTTP address is
`http://localhost:58477`, with Swagger at `/swagger` and health at `/health`.
The Worker is a separate process; this API debug setup does not start it.

If dependencies are stopped, rerun the setup script. To inspect them:

```powershell
docker compose -f compose.yaml -f compose.debug.yaml ps
```

## First business walkthrough

1. In Catalogue, create a product with optional brand/source information; leave unknown fields blank.
2. Create a supplier and a packaging/SKU variant, for example 5 kg at 55,000 VND. Initial stock is zero.
3. In Purchase receipts, create a draft for six bags at 30,000 VND cost, review it, then confirm.
4. In Sales, create a two-bag draft and confirm. The frozen sale total is 110,000 VND and current stock is four bags.
5. Inventory shows low stock at the default threshold of five. Replenishment shows a suggestion for 26 bags toward the default target of 30 after the Worker handles the event.
6. Stock movements show both changes. Creating a draft alone has no stock effect.

The automated browser smoke creates clearly labelled demo catalogue records and documents using this walkthrough. They remain in the local database as reviewable demo data; the script does not delete business history.

## Development and verification

Install .NET SDK 9.0.304 or a later compatible .NET 9 SDK selected by `global.json`, and Node 22.12+ for native development. Docker builds use SDK 9.0.310. The test script prefers the installed `dotnet`; a .NET 10-only SDK cannot run the .NET 9 test host.

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

Current application targets .NET 9, pins EF/Microsoft packages 9.0.22 and Npgsql provider 9.0.4, AutoMapper 16.2.0, gRPC 2.84.0 and StackExchange.Redis 2.10.1. Frontend dependency resolution is recorded in `package-lock.json` and installed with `npm ci`.

AutoMapper license configuration accepts `AutoMapper__LicenseKey` / the Compose `AUTOMAPPER_LICENSE_KEY` environment value. Review the [official license configuration](https://docs.automapper.io/en/stable/License-configuration.html) for eligibility and use. Missing-key messages remain visible; no license key or purchase is supplied by this repository.

Whole-VND unit prices/costs are capped at 1,000,000,000,000, bag quantities at 1,000,000 per line and documents at 100 distinct SKU lines. Document totals are capped at 9,000,000,000,000,000 VND so integer totals remain exact in browser numbers. Package amounts support at most two decimal places. PostgreSQL uses `numeric(18,0)` for money and rejects negative stock.

## Remaining submission work

- Team members must review the assignment rubric, assign named responsibilities and perform the final presentation/demo.
- The local Compose setup is not an internet-facing production deployment. PostgreSQL archives share the host failure domain.
- A token remains in browser memory only; refresh signs out. Existing sessions are rejected after account changes.
- The stock-movement endpoint filters by SKU; additional date, actor and movement-kind filters remain a possible usability enhancement. Catalogue list filters are narrower than the separate advice criteria; see the API map in the system design.

Product/SKU/supplier selectors query and page through the server; sales and receipt lists expose code, status, date and actor filters. The replenishment inbox supports search and pagination. See [operations workflows](operations-workflows.md) and [verification](verification.md) for API contracts and test boundaries.

## Source organization

Business-specific services, contracts and repository ports are grouped within each existing layer. React pages, models and forms are grouped by feature. See [code-organization.md](code-organization.md) for the folder map and rules for new code. The restructuring preserves HTTP routes, EF model/migrations, stock transactions and source-review rules.
