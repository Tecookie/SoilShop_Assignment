# Design Verification — 23 October 2026

This report distinguishes tested design artifacts from an application implementation. The PostgreSQL tests use disposable isolated containers, not the user's running databases. The test containers and their own anonymous volumes are removed afterward.

## Database execution

`docs/tools/verify_postgres_design.py` executed the generated baseline against PostgreSQL 16 and passed the following checks; details are in [runtime-checks.json](database/runtime-checks.json):

- All 18 tables, FKs, CHECK constraints and indexes execute on a fresh database.
- Two live connections compete for the last bag: one confirmation commits and the other reports insufficient stock.
- The result has stock zero, one Confirmed sale, one ledger movement and one outbox event.
- Repeating the successful confirmation key creates no second deduction.
- A multi-SKU sale with one unavailable SKU rolls back all earlier deductions and remains Draft.
- A stale stock-version predicate affects zero rows; the database rejects negative stock.
- A stale physical count becomes RecountRequired without changing stock.
- A current count changes stock once; repeated approval is idempotent and produces one adjustment/ledger entry.
- A second active suggestion for a SKU violates the partial unique index.
- Metadata cannot be marked reviewed without a recorded source.
- A custom-format pg_dump archive restores into a separate fresh database with matching sample sale/ledger counts.

The SQL functions in [concurrency-demo.sql](database/concurrency-demo.sql) are prototypes for those checks only. They are not application stored procedures or EF migrations; they do not implement HTTP authorization, gRPC, AutoMapper, metadata ranking or every business validation.

## Diagram and document verification

- 13 self-contained HTML sources: four C4 figures (one C1, three C2 perspectives), two conceptual ERDs and seven physical subsystem figures.
- Each has a standalone SVG and a PNG exported at 2× its declared canvas size.
- Headless Chrome render checks passed for SVG accessibility, text within the viewBox and local horizontal scrolling at a 390px viewport without whole-page overflow.
- diagram-design self-checks are run on all figure sources; the exported images are visually inspected for labels/routes.
- Structurizr `validate` executed successfully in the official Docker image for [workspace.dsl](diagrams/workspace.dsl).
- Static checks validate local file links, schema FK names/types, unique constraint names, SVG marker references, image dimensions and expected C4 view levels. Machine-readable outputs are [static-checks.json](diagrams/static-checks.json) and [render-checks.json](diagrams/render-checks.json).

## Still pending during implementation

React/API/gRPC/Worker source, EF migrations, application Dockerfiles/Compose, actual JWT/role enforcement, quote/ranking correctness, Redis crash/retry behavior, file-retention failure cases and full browser workflows. Source-backed metadata review invalidation and stock-count permissions also need application tests. A SQL prototype pass is not a deployed application test pass.
