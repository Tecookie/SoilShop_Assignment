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

## Broker context and delivery constraints — round 4

- The application serves a broker organization, not the developer's internal system.
- Developers provide inventory and information to the broker; supplied details may be incomplete.
- Unknown villa area details may remain empty. The user is open to additional changeable data; the exact extension mechanism is unresolved.
- Roles accepted: Admin manages accounts and all catalogue data; Employee manages catalogue data and enters prices. No public registration.
- Unit management statuses: Managed, Unmanaged, Sold.
- Unmanaged includes units no longer assigned to this broker and handed to another party when the developer changes inventory allocation. Exact transition/reactivation rules remain open.
- Team: 3 people. Time remaining: approximately 2–3 weeks.
- Frontend: a simple React web application.

## Confirmed functionality and architecture — round 5

- Required unit identity: code, type, project, and building/floor for apartments.
- Descriptive attributes may be unknown and left empty.
- Additional unit information uses editable name/value pairs; no filtering or formulas on these fields in the initial scope.
- New units default to Managed. Employees may set Unmanaged or Sold, select multiple units to stop managing, and reactivate an existing unit when reassigned.
- Status corrections require a short reason. Keep status history including actor, timestamp, old/new status and reason.
- Accepted bulk workflow: React input → REST API → independent gRPC numbering service → preview → confirmation → Redis Streams → .NET Worker → database → result visible in React.
- Maximum 500 units per batch. Creation is all-or-nothing; processing status and created count are visible.
- Accepted stack: React, ASP.NET Core, EF Core, SQL Server, Redis Streams, .NET gRPC, Docker Compose. Exact supported versions remain to be verified.
- Employees can see the broker organization's full catalogue.
- Search/filter by unit code, developer, project, building, type, management status, presence of price, and price range; sorting and pagination.
- Proposed additional scheduled job (not yet confirmed): daily per-project catalogue summaries, including management-status counts and units missing a price, with a manual trigger for demonstration.

## Confirmed documentation and scheduled jobs — round 6

- Daily catalogue summary job accepted: 00:05 Asia/Ho_Chi_Minh, with a manual trigger. Dashboard displays the summary timestamp and counts rather than implying real-time results.
- Database backup job requested. Storage, retention, timing, manual trigger and restore scope remain to be confirmed.
- Developer: code/name required; contact details optional.
- Project: code/name/developer required; address/description optional.
- Building: code/name/project/floor count required.
- Delete developers/projects/buildings only when they have no child records.
- Preserve units with status history; use Unmanaged. Mistakenly created units without status history may be deleted.
- Price is the total unit price in VND, optional at creation.
- Final documentation language: English.
- Diagrams: PNG and SVG, C4 L1/L2, conceptual ERD, physical database diagram.
- User requested a different palette from the default cream look. Selected direction: white background, navy text, cyan accents; exact tokens will be recorded with the diagram source.

## Backup trigger refinement — round 7

- User requires backup after data changes, in addition to the proposed daily backup.
- Proposed interpretation for final confirmation: enqueue a backup after each successful logical business-data transaction (catalogue create/edit/delete, price edits, status changes, additional information, account changes).
- A bulk operation is one transaction and one backup request, not one request per row.
- Backups run asynchronously and sequentially after commits; backup/job bookkeeping must not recursively trigger backups.
- A change-triggered backup captures database state when the backup runs, possibly including later commits; it is not an exact snapshot of each intermediate mutation.
- Proposed retention: 7 successful scheduled backups plus 7 successful change-triggered/manual backups in a separate Docker volume. Failed attempts do not count toward retention.
- Proposed manual Admin trigger and README restore demonstration remain part of the final scope confirmation.

## Confirmed business model — round 2

- Each project belongs to one developer.
- Projects may contain buildings/blocks. Apartments belong to a building; townhouses and villas belong directly to the project.
- Unit types: Apartment, Townhouse, Villa. High-rise/low-rise describe buildings, not unit types.
- Building floor count differs from an apartment's floor number and a townhouse/villa's own floor count.
- Main creation workflow: bulk creation by unit type, with support for generating a sequence of unit codes.
- Prices are assigned after units are created; creation does not require entering each unit's price.
- Exact sequence rules, default fields, preview behavior, and pricing workflow remain open.

## Confirmed business rules — round 3

- Apartments: select building, start/end floor and units per floor; generate codes such as `A-0101` through `A-0510`.
- Townhouses/villas: enter prefix, starting number and quantity; generate codes such as `NP-001` or `BT-001`.
- Preview generated units before saving. Duplicate unit codes within the project are reported before saving.
- A batch shares entered attributes; individual units can be edited after creation.
- Apartment fields: usable area, bedroom count, bathroom count.
- Townhouse fields: land area, building footprint area, own floor count, bedroom count, bathroom count.
- Villa area details must not prevent creation when unknown. Interpretation pending clarification: retain optional area fields rather than removing them.
- Newly created units have no price. An employee enters a unit's price later.
- Batch pricing, price formulas, and price history have not been accepted as features.

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

- Final confirmation of backup transaction granularity, asynchronous snapshot semantics, retention, restore scope and permissions.
- Final treatment of batch numbering and concurrency/error behavior in the design document.
- Final scope confirmation before writing the complete design document.
