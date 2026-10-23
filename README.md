# Potting Soil Shop — PRN232

Internal application for a shop that purchases pre-packed potting-soil bags and records stock receipts, counter sales and stock adjustments.

**Current state: approved design complete; application implementation pending.** PostgreSQL schema, SQL concurrency/count prototypes and isolated dump/restore have been tested. This is not a deployed application.

## Documentation

- [Confirmed scope and technology](docs/gardening-discovery.md).
- [System design](docs/system-design.md): Context, Problems, Solutions, Actors, Features, C4 C1/C2, Technology, conceptual ERD and Physical DB.
- [Diagram gallery](docs/diagrams/index.html): C1, full C2 and focused synchronous/async C2 views, plus conceptual/physical models.
- [PostgreSQL schema](docs/database/schema.sql) and [data dictionary](docs/database/data-dictionary.md).
- [Backup/restore guide](docs/backup-and-restore.md) and [verification report](docs/verification.md).
- [Domain glossary](GLOSSARY.md).
- [Previous real-estate design archive](docs/archive/real-estate/system-design.md), superseded by the new domain.

## Intended stack and deployment

React/Vite/TypeScript with plain CSS, ASP.NET Core, EF Core with Npgsql, PostgreSQL, AutoMapper, Redis Streams, independent gRPC calculation and product-advice service, .NET Worker and Docker Compose. Use migrations and exported PostgreSQL scripts. No customer storefront or payment-provider integration.

## Submission checklist

- [x] New business scope and technology decisions.
- [x] Revised English design, C4 L1/L2, conceptual ERD and PostgreSQL physical model.
- [x] PostgreSQL schema and backup/restore guide with isolated SQL/restore checks.
- [ ] Application source, reviewed EF migrations and demo seed data.
- [ ] Dockerfiles and Docker Compose for all services/volumes.
- [ ] OpenAPI/Swagger and working installation/deployment steps.
- [ ] Integration checks, restore drill and end-to-end demonstration.
- [ ] Named team responsibilities and presentation.

Repository publication is handled by the user.
