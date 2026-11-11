# PRN232 assignment traceability

This table maps the three-page [Final Assignment brief](../PRN232%20-%20Final%20Assignment.pdf) to the current repository. Automated checks are evidence of the paths described; the team still needs to present the system and confirm the submission against the rubric.

| Brief / weight | Repository evidence | What to show in the final demonstration |
| --- | --- | --- |
| Architecture and design · 20% | [System design](system-design.md), [C4 diagrams](diagrams/index.html), [ERD and database](database/data-dictionary.md), [code ownership](code-organization.md) | Admin/Employee, six containers, request and event paths, Product/SKU and transaction relationships. |
| REST API · 20% | `src/SoilShop.Api`, Application services and Infrastructure repositories; OpenAPI JSON at `/api/openapi/v1.json`; [trading](trading-workflows.md) and [operations](operations-workflows.md) | JWT login, protected endpoint, core Product/Draft CRUD, search/filter/sort/pagination and meaningful 400/401/403/409 responses. |
| Background job · 10% | `src/SoilShop.Worker/Inventory`, `Reporting`, `Backups`; [operations guide](operations-workflows.md) | Low-stock reconciliation, queued summary and Admin manual backup progressing to a result. |
| Message broker · 15% | API `Hosting/OutboxRelay.cs`, Infrastructure `Messaging/RedisBus.cs`, Worker `InventoryConsumer.cs` | Show outbox, Redis Stream publish and Worker consumption/ACK. Use the [isolated recovery evidence](operations-checks.json) for failure handling. |
| gRPC · 15% | `src/SoilShop.Contracts/Protos/calculation.proto`, `src/SoilShop.Calculation`, Infrastructure calculation client | Show API calls to the independent service for quote totals and explainable product advice. |
| Docker/cloud · 10% | [compose.yaml](../compose.yaml), [Dockerfile](../Dockerfile), [Worker Dockerfile](../Dockerfile.worker), frontend Dockerfile | Start Web, API, Calculation, Worker, PostgreSQL and Redis; show health and communication. Compose is the selected deployment option. |
| Documentation/presentation · 10% | [README](../README.md), migration [SQL](database/ef-migrations.sql), [demo walkthrough](demo-walkthrough.md), [verification](verification.md) | Explain setup, architecture, tests and one end-to-end workflow. Name the actual team members and responsibilities before submitting. |

The technical checklist in the brief is represented by .NET 9, EF Core/Npgsql,
PostgreSQL, dependency injection in `src/SoilShop.Api/Program.cs`, environment
configuration, structured logs/exception middleware, JWT, REST, gRPC, Redis Streams,
Worker services and Docker. OpenAPI is available through the Web proxy; see the
[run guide](implementation.md) for Swagger UI's API-host boundary.

## Team responsibilities

The repository documents technical ownership by feature in [code organization](code-organization.md). The assignment asks for **named team member responsibilities**; the team must add its actual names and agreed contributions here or in the README before submission. Names have not been inferred from Git history.

## Final presentation checklist

1. Build and start the six services using the [run guide](implementation.md).
2. Walk through architecture, login, a receipt, a sale and source-backed advice.
3. Show the API's gRPC request and Redis producer/consumer logs; show a background summary/backup result.
4. Explain last-bag protection, stale stock count, role checks and a recovery example using [verification evidence](verification.md).
5. Review the API contract, migration SQL, source code, Docker files and team responsibilities before the live presentation.

The guide intentionally distinguishes a completed automated check from a presentation the team has not yet performed.
