# Code organization and business ownership

The existing layered architecture is retained. Each layer groups code by the business area that owns it. HTTP contracts, CLR namespaces, EF entities/migration IDs and database transaction boundaries remain stable.

## Backend layout

```text
src/
  SoilShop.Api/
    Controllers/{Authentication,Catalogue,Advice,Trading,Inventory}/
    Authentication/       JWT token implementation
    Hosting/              outbox relay hosted service
    Middleware/           exception-to-ProblemDetails translation
    Program.cs            application composition and middleware pipeline
  SoilShop.Application/
    Authentication/       login, current-user contracts and user repository port
    Catalogue/            product/SKU/supplier contracts, validation, repository port
    Advice/               metadata review, matching orchestration, candidate/client ports
    Trading/              receipt/sale drafts, quotes, confirmation, cancellation, DTOs
    Inventory/            ledger and replenishment queries
    Calculation/          total calculation client contract
    Common/               paging, business exception and AutoMapper read mappings
  SoilShop.Domain/
    {Authentication,Catalogue,Advice,Trading,Inventory,Messaging,Reporting,Backups}/
  SoilShop.Infrastructure/
    {Authentication,Catalogue,Advice,Trading,Inventory}/
    Persistence/          DbContext, design-time factory, database/bootstrap setup
    Migrations/           existing generated migrations, deliberately retained
    Calculation/          gRPC client and Protobuf mapping
    Messaging/            Redis transport and transactional outbox intent creation
    Seeding/              explicit fictional demo imports
  SoilShop.Calculation/
    Advice/               pure advice ranking rules
    Totals/               pure total validation/calculation rules
    Services/             gRPC method adapters
  SoilShop.Worker/
    Inventory/            Redis consumer and current-state reconciliation
```

| Responsibility | Application owner | Infrastructure owner |
| --- | --- | --- |
| Product, packaging and supplier maintenance | CatalogueService / ICatalogueRepository | CatalogueRepository |
| Login and account lookup | AuthService / IUserRepository | UserRepository and PasswordVerifier; JWT implementation in API |
| Metadata source review | SoilMetadataService / ISoilMetadataRepository | SoilMetadataRepository |
| Product matching | ProductAdviceService / IAdviceCandidateRepository / IProductAdviceClient | AdviceCandidateRepository and CalculationClient |
| Receipt and sale lifecycle | TradingService / ITradingRepository | TradingRepository, TradingRepository.Drafts, TradingRepository.Cancellation |
| Ledger and restock inbox reads | InventoryService / IInventoryRepository | InventoryRepository |

The broad ShopService/IShopRepository pair has been removed. Controllers call the service for their area; they do not contain stock/pricing decisions. TradingService owns draft normalization, input validation, quote orchestration and cancellation validation. TradingRepository retains document/SKU locking, optimistic checks, atomic stock/ledger/outbox writes and rollback. Its partial files describe phases of the same aggregate lifecycle, rather than unrelated repositories.

Catalogue and Trading repositories share the scoped DbContext and use InventoryEvents to append an event intent inside their existing transactions. This helper neither commits independently nor contacts Redis. Auth, catalogue and inventory reads no longer depend on a repository that exposes all trading mutations.

Root CLR namespaces are deliberately retained while folders express ownership. Moving entities must not change EF identity or rewrite historical migration designers. Common contains genuinely shared paging/errors/read mappings; feature DTOs and repository interfaces stay beside their service. Domain Reporting/Backups entities express the approved schema, not completed application workflows.

## React layout

```text
src/soilshop-web/src/
  main.tsx
  app/App.tsx
  features/
    authentication/       login page and session/user types
    catalogue/            catalogue page, product/SKU/supplier types
      components/         CatalogueForm, ProductFields, SkuFields
    advice/               advice page, metadata types and option data
      components/         metadata editor, facts display, option rendering
    trading/              workflow page, document/quote types, local draft model
      components/         draft, quote review, deletion and cancellation panels
    inventory/            stock, ledger and replenishment screens/types
  shared/
    api/client.ts         authenticated HTTP transport and ProblemDetails errors
    components/           Field, Notice, Empty, Pagination
    lib/format.ts         VND and Vietnam-time formatting
    types/paging.ts       paginated response envelope
    styles/global.css     existing design tokens, layout and common control styles
```

Pages coordinate HTTP/state and compose controlled components. Product fields and SKU fields have separate components. Trading's draft, quote, delete and cancellation panels have explicit props; pending state, action keys and HTTP orchestration remain in the page. Server validation remains authoritative. Catalogue reuses the Advice metadata editor; shared controls contain no stock-changing decisions. The HTTP client's typed quote error extension remains a type-only dependency on the Trading contract.

## Tests and adding code

Tests use Advice, Calculation, Seeding and Trading folders, with the shared WebApplicationFactory in Support. Existing test identities/assertions remain intact. Calculation tests call pure TotalsRules; the gRPC adapter delegates to those rules. Existing broad HTTP integration scenarios remain together as TradingTests plus TradingWorkflowTests so shared setup and historical test names are preserved.

For a new behavior, identify its owner first. Add its DTO and repository port to that Application feature, implementation to the matching Infrastructure folder, and controller action to the API feature. Add UI contracts/components beside its React page; move a component to shared only when it has no feature-specific responsibility. Keep database access out of Application/React, source review out of generic UI, and generated migrations outside manual refactors. Do not recreate catch-all Contracts, ShopService or ShopRepository files.

No C4 container boundary changed, so the existing C1/C2 figures remain applicable.

## Added operational owners

`StockWorkflowService` / `StockWorkflowRepository` own count, adjustment and
suggestion conversion transactions. `AdministrationService` /
`AdministrationRepository` own accounts and supplier updates. `OperationsService`
and `OperationsRepository` queue and expose summary/backup runs;
`SummaryProcessor`, `SummaryWorker` and `BackupWorker` implement independent
background loops. React adds `inventory/StockCountsPage`, `operations/`,
`administration/` and a reusable paginated `shared/components/SearchSelect`.
See [operations workflows](operations-workflows.md) for contracts and
[verification](verification.md) for runtime boundaries.

## Reviewed branch and integration

Reviewed `feature/advice-receipts-sales` at 3555489. It adds draft replacement/deletion, reviewable gRPC quotes, refreshed quote conflicts, per-line price revalidation and Admin whole-document cancellation with reversal audit/outbox. API role restrictions, expected versions/action keys and transaction rollback were retained. The branch originally used .NET10 and lacked main's later advice fixtures; main was merged into it first to retain the requested .NET9 runtime and advice demo fixes. Merge conflicts combined both verification histories and kept both SKU-scoped browser selection and the new Review total step.

See [verification.md](verification.md) and [code-organization-checks.json](code-organization-checks.json) for the checks performed before the local merge to main.
