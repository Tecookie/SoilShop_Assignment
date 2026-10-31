# Potting Soil Shop — PRN232

Internal application for a shop that purchases pre-packed potting-soil bags and records stock receipts, counter sales and stock adjustments.

**Current state: first runnable application increment.** JWT login, catalogue, receipt/sale confirmation, stock history, gRPC totals and Redis/Worker low-stock suggestions run together in Docker Compose. The remaining approved workflows are listed in the [implementation guide](docs/implementation.md).

## Run

With Docker Desktop in Linux-container mode and PowerShell 7:

```powershell
./scripts/setup.ps1
docker compose up -d --build
```

Open <http://localhost:8080>. Sign in with `BOOTSTRAP_USERNAME` / `BOOTSTRAP_PASSWORD` from the ignored root `.env`. Setup generates local secrets and preserves existing configuration. Credentials are not committed. See the [run/test guide](docs/implementation.md) for prerequisites, limits and subsequent work.

To populate the six product groups with reviewable demo data, run `./scripts/seed-demo.ps1`. It adds 8 products, 16 SKUs, 3 suppliers and sample receipt/sale documents without duplicating stock on reruns. See [demo data and classification](docs/demo-data.md).

## Source

`SoilShop.sln` contains Domain, Application, Infrastructure, API, Calculation and Worker projects plus tests. React/Vite/TypeScript lives in `src/soilshop-web`. The API applies reviewed EF migrations on startup; use [ef-migrations.sql](docs/database/ef-migrations.sql) for the exported application schema.

## Documentation

- [Confirmed scope and technology](docs/gardening-discovery.md).
- [Implemented features, installation and tests](docs/implementation.md).
- [End-to-end demo flow and technology evidence](docs/demo-walkthrough.md).
- [System design](docs/system-design.md): Context, Problems, Solutions, Actors, Features, C4 C1/C2, Technology, conceptual ERD and Physical DB.
- [Diagram gallery](docs/diagrams/index.html): C1, full C2 and focused synchronous/async C2 views, plus conceptual/physical models.
- [PostgreSQL schema](docs/database/schema.sql) and [data dictionary](docs/database/data-dictionary.md).
- [Backup/restore guide](docs/backup-and-restore.md) and [verification report](docs/verification.md).
- [Domain glossary](GLOSSARY.md).
- [Previous real-estate design archive](docs/archive/real-estate/system-design.md), superseded by the new domain.

## Intended stack and deployment

React/Vite/TypeScript with plain CSS, ASP.NET Core 9, EF Core 9 with Npgsql, PostgreSQL, AutoMapper, Redis Streams, independent gRPC calculation and product-advice service, .NET Worker and Docker Compose. Use migrations and exported PostgreSQL scripts. No customer storefront or payment-provider integration.

## Submission checklist

- [x] New business scope and technology decisions.
- [x] Revised English design, C4 L1/L2, conceptual ERD and PostgreSQL physical model.
- [x] PostgreSQL schema and backup/restore guide with isolated SQL/restore checks.
- [x] Initial application source, reviewed EF migrations and browser-created demo records.
- [x] Dockerfiles and Docker Compose for the current six-service increment.
- [x] OpenAPI JSON and working local installation/deployment steps.
- [ ] Remaining approved features, including advice, counts, cancellation, summaries and backups.
- [ ] Integration checks, restore drill and end-to-end demonstration.
- [ ] Named team responsibilities and presentation.

Repository publication is handled by the user.

See [soil metadata and advice](docs/soil-advice.md) for nine-factor filtering, source review and the gRPC recommendation demo.
