# Verification — latest increment 31 October 2026

This report distinguishes tested design artifacts from an application implementation. The PostgreSQL tests use disposable isolated containers, not the user's running databases. The test containers and their own anonymous volumes are removed afterward.

## .NET 9 compatibility — 31 October 2026

- All projects target `net9.0`; SDK 9.0.310, direct Microsoft/EF packages and EF tool 9.0.22, Npgsql EF provider 9.0.4. API, calculation and Worker containers report ASP.NET Core/.NET 9.0.22.
- Native solution build passed with zero warnings/errors, and all **20 tests passed** on .NET 9 using an isolated PostgreSQL database. Dependency audit reported no known vulnerable direct/transitive packages from current sources.
- EF9 reports no pending model changes. Existing migration IDs/history were preserved; the idempotent SQL export was regenerated with EF9. No schema-reset migration was added.
- Before/after fingerprints matched for catalogue/document/ledger counts and SKU stock, stock versions and prices when switching the existing Compose database to the .NET 9 hosts.
- All five advice, six seed and nine trading browser checks passed against the rebuilt Compose stack. Independent gRPC totals/advice and Redis/Worker replenishment were exercised. The trading smoke now selects its own SKU's document rather than clicking the first existing draft before list refresh.
- Existing user-created launch settings were preserved outside this change.

Evidence: [net9-checks.json](net9-checks.json). Transient build/test/browser logs remain under ignored `test-results/`.

## Nine-factor soil advice — 31 October 2026

- All **20 application tests pass**, zero skipped. New tests cover nullable metadata, validation, version conflicts, source-required review and invalidation, candidate review/stock/budget eligibility, strict/incomplete rules, comparable EC, pH interval matching and top-three ordering. Integration tests use isolated PostgreSQL; the test advice adapter runs the actual Protobuf mapping and rule implementation in-process.
- The actual Compose stack rebuilt successfully. Five feature browser checks passed through React/Nginx/API and the independent gRPC container, with zero page/console errors. A sourced, explicitly reviewed fictional product returned 12/12 selected-criteria points; calculation logs confirmed HTTP/2 `RecommendProducts` and `advice-v2`.
- Checks cover the nine-factor metadata form, explicit review, false versus unknown sample availability, reasons/source display, 320/768/1440px page widths, sale-draft creation without stock decrement, Employee write denial, budget exclusion, differing EC methods, opt-in missing data and edit invalidation. The synthetic fixture's review was cleared after verification.
- Six seed-browser regression checks and nine core-trading browser checks also passed. Existing seed metadata remains unknown and unreviewed.
- The additive EF migration and idempotent SQL export include eleven nullable columns and three CHECK constraints. The 18-table design baseline executed in an isolated PostgreSQL container; design prototype checks and restore test passed again. This does not implement application backup jobs or stocktaking.
- All 13 diagram render/static checks passed. Updated physical catalogue/tag diagrams and dictionary include the new fields.

Evidence: [feature checks](soil-advice-checks.json), [metadata editor](screenshots/soil-metadata.png), [matching result](screenshots/soil-advice.png). Rules and staff demo are in [soil-advice.md](soil-advice.md). Transient logs/TRX remain in ignored `test-results/`.

## Product classification and demo seed — 24 October 2026

- Product names and displayed type labels now use Vietnamese. A guarded data migration translates existing English fixture names while preserving customized names. SKU identity/stock/version/price hashes remained unchanged, Docker builds passed, and all six read-only browser checks passed again after translation. Future seeds and browser fixtures also use Vietnamese names.
- All **14 application tests pass**, including validated type filters and a seed test that interrupts calculation, resumes the import, repeats it without duplicates and preserves subsequent staff price/stock changes.
- Six read-only Chrome checks pass for all six inventory type filters, Unclassified empty state, catalogue/edit selection, seeded stock states, four viewport widths and Employee navigation. No browser console/page errors were recorded.
- The existing nine-check browser trading workflow also passes after the change.
- The actual local Compose import created eight products, 16 SKUs, three suppliers, two confirmed/one draft receipts, three confirmed/one draft sales and a configured Employee account. A second CLI import returned `AlreadyApplied`; stock signatures and document/movement/outbox counts stayed identical.
- Optional measurements and review attribution remain unknown. Six product groups are catalogue descriptions, not certified advice. Existing data remains intact; see [demo-data.md](demo-data.md).
- Source schema, EF migration/export, dictionary and physical diagram sources were updated. All 13 diagram renders passed.

Evidence: [seed-checks.json](seed-checks.json), [seeded inventory](screenshots/seed-inventory.png) and [filtered catalogue](screenshots/seed-catalogue.png). Earlier checks below describe the initial increment separately.

## Initial application increment

The application now runs as six Compose services. See [implementation.md](implementation.md) for exact feature coverage and pending work.

- `dotnet build SoilShop.sln` passed with zero warnings/errors; all four application Docker images built successfully.
- `scripts/test.ps1` passed **12 tests**, using real disposable PostgreSQL 16 for HTTP integration. Coverage includes last-bag concurrent sales, duplicate confirmation, multi-line rollback, frozen prices, duplicate codes, stale catalogue edits, forbidden Employee writes, active-account checks, unexpected fields, nonwhole money, AutoMapper configuration and totals validation/overflow.
- `scripts/browser_smoke.py` passed **nine workflow checks** against the actual Compose services: JWT login; Admin catalogue/supplier/SKU creation; six-bag receipt; two-bag sale at 110,000 VND; stock four; low-stock suggestion 26; two ledger entries; responsive viewports; sign-out and zero browser console/page errors. Form labels were corrected after the first browser run exposed ambiguous select names.
- The API-to-calculation service calls completed using HTTP/2 gRPC, confirmed by calculation-host request logs. API outbox publication and Worker consumption are visible in their logs.
- Two additional Redis entries carrying the same test event ID were consumed; a direct PostgreSQL query still returned exactly one active suggestion for the demo SKU.
- OpenAPI JSON responds at `/api/openapi/v1.json` through Web. Only Web exposes a loopback host port.
- `npm audit` and the .NET transitive vulnerable-package check reported no known vulnerabilities from their current sources on the test date.

Machine-readable browser results are in [application-checks.json](application-checks.json). Screenshots show [desktop inventory](screenshots/inventory-desktop.png), [mobile inventory](screenshots/inventory-mobile.png) and [replenishment](screenshots/replenishment.png). Full transient test reports are generated under ignored `test-results/`.

HTTP integration tests replace the calculation network boundary with a deterministic implementation. The browser run separately exercises the real gRPC host and Redis/Worker. These checks establish the initial increment, not every target feature or failure/recovery case.

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

Stock-count/adjustment permissions, cancellation/reversal, draft editing/deletion, account management, suggestion conversion, scheduled/manual summaries/backups, backup retention failure cases and Redis outage/recovery tests remain pending. The earlier physical-count and backup checks below exercise SQL/design prototypes only; those application workflows are not implemented yet.
