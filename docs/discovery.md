# Requirements discovery

Status: In progress. Business model and architecture are not yet finalized.

## Confirmed scope

- Internal application for employees managing a real-estate catalogue.
- Catalogue covers developers, projects, and property units.
- Different property types need different information; exact fields, defaults, and creation workflow remain open.
- Payment features are out of scope. Other sales workflows require explicit scope discussion.
- Local Git repository; the user handles remote publication.
- Documentation in `docs/`; images in `docs/diagrams/images/`.
- C4 diagrams limited to Level 1 and Level 2.

## Assignment requirements

Source: `PRN232 - Final Assignment.pdf`, pages 1–3.

- At least one ASP.NET Core REST API using .NET 8 or later.
- CRUD for core entities and API → Services → Repository layering.
- JWT authentication; searching, filtering, sorting, and pagination where appropriate.
- EF Core and a relational database such as SQL Server.
- At least one scheduled or asynchronous background processing service.
- Kafka or Redis (Pub/Sub or Streams), demonstrating producer and consumer.
- An independent gRPC service called by the REST API.
- Dependency injection, configuration management, logging, exception handling.
- Docker containerization and deployment using Compose or a public cloud.
- Source code, database scripts/migrations, Dockerfiles, Compose where applicable, Swagger/OpenAPI.
- README covering description, architecture, technology, installation, deployment, and team responsibilities.
- Demonstrate all components communicating and an end-to-end business workflow.

## Planned document contents

Context; Problems; Solutions; Main Actors; Main Features; System Architecture (C4 L1–L2); Technology; Conceptual ERD; Physical Database.

## Open decisions

- Developer/project/unit relationships and any building/block hierarchy.
- Property types, fields, and meaning of number of floors.
- Defaults versus user-entered values; single versus batch creation.
- Roles, permissions, lifecycle, deletion rules, and catalogue searches.
- Business use of gRPC, messaging, and background processing.
- Technology choices, frontend scope, and team/time constraints.
