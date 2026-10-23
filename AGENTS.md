# Assignment agent instructions

Build a simple internal PRN232 application for a shop selling pre-packed potting-soil bags purchased from suppliers. No customer-facing storefront, soil mixing, repacking or loose-weight sales. Follow the requirements in `PRN232 - Final Assignment.pdf`. Avoid expanding the project into a production release.

Requirements discovery using Matt Pocock's grilling and domain-modeling skills is recorded in `docs/gardening-discovery.md` and the root `GLOSSARY.md`. The completed approved design is `docs/system-design.md`, including product advice with source-backed metadata, replenishment suggestions and stocktaking approval. Consult it before implementation. The previous real-estate artifacts are historical material under `docs/archive/real-estate/`; do not treat that archive as approved decisions for the new domain.

## Technology

- PostgreSQL with EF Core and the Npgsql provider; use EF migrations and export SQL scripts.
- React + Vite + TypeScript; omit Ant Design and start with plain CSS/reusable React components.
- Use AutoMapper for entity/DTO mapping, keeping trusted stock/status/role/totals under Service control.
- Layered ASP.NET Core REST API, independent gRPC calculation service, .NET Worker, Redis Streams and Docker Compose.
- Backup daily plus manual Admin trigger; retain 7 successful PostgreSQL backups. No backup after every mutation.

## Agent skills

### Issue tracker

Track issues and specs as local Markdown in `.scratch/`. See `docs/agents/issue-tracker.md`.

### Domain docs

Use a single context: root `GLOSSARY.md` and `docs/adr/`. See `docs/agents/domain.md`.

## Documentation and diagrams

- Save project documentation in `docs/`.
- Use c4-architecture for the C4 model and diagram-design for diagram presentation.
- Draw C4 Level 1 (System Context) and Level 2 (Container) only.
- Save diagram sources in `docs/diagrams/` and image exports in `docs/diagrams/images/`.
- The user handles repository publication. Do not publish or push without a later instruction.
