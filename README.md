# Broker Property Catalogue

PRN232 assignment design for an internal real-estate broker catalogue. The team has three members and approximately two to three weeks for implementation.

**Current state: requirements and design complete; application implementation pending.** No application build, Docker deployment or backup execution is claimed by this repository yet.

## Documentation

- [System design](docs/system-design.md): Context, Problems, Solutions, Main Actors, Main Features, C4 L1/L2, Technology, conceptual ERD and Physical DB.
- [Physical SQL baseline](docs/database/schema.sql) and [data dictionary](docs/database/data-dictionary.md).
- [Backup and restore guide](docs/backup-and-restore.md).
- [Diagram gallery](docs/diagrams/index.html); [PNG/SVG exports](docs/diagrams/images/).
- [C4 source](docs/diagrams/workspace.dsl).
- [Confirmed interview decisions](docs/discovery.md) and [domain glossary](GLOSSARY.md).

## Intended stack and deployment

React/TypeScript, ASP.NET Core and EF Core, SQL Server, Redis Streams, independent .NET gRPC numbering service, .NET Worker and Docker Compose. Installation instructions and actual Compose commands will be added with the implementation; the topology is documented in the design. Diagram sources and schema are design artifacts, not a runnable application.

## Submission checklist

- [x] Approved scope and design documentation.
- [x] C4 Level 1/2, conceptual and physical data diagrams.
- [x] SQL baseline and backup/restore templates.
- [ ] Application source, reviewed EF migrations and demo seed data.
- [ ] Dockerfiles and Docker Compose for all services/volumes.
- [ ] OpenAPI/Swagger and working installation/deployment steps.
- [ ] Integration checks, restore drill and end-to-end demonstration.
- [ ] Named team responsibilities and presentation.

Repository publication is handled by the user.
