# Gardening Sales — Requirements Discovery

Status: Business scope Q1–Q17 and three enrichment workflows approved. On 23 October 2026 the user requested that the additions be documented and design begin, with richer C4 Level 1/2 diagrams. Earlier SQL Server references are superseded by PostgreSQL.

## Approved enrichment — 23 October 2026

- Explainable product advice: staff choose needs, package size and budget; filter active in-stock SKUs, rank using known product metadata and return matching reasons. No AI model or invented horticultural facts. Extend the existing stateless gRPC host with advisory methods.
- Structured optional metadata: brand/manufacturer, ingredients, intended plant/use tags, drainage, water retention, supplied pH range, care/storage notes, source and review information. Unknown stays unknown. Package amount/unit is SKU-level; no mass/volume conversion.
- Replenishment suggestions: per-SKU warning threshold, target stock and preferred Supplier. Worker upserts one active suggestion per SKU, using max(0, target minus current stock); employees may convert it to a purchase-receipt draft. Stock changes only at receipt confirmation; no external supplier-order integration.
- Stocktaking with approval: employee captures current stock/version and records an actual bag count/reason; Admin approves the discrepancy into an immutable stock adjustment. Any intervening stock movement invalidates the captured version and requires recounting. Approval must not overwrite newer stock or apply twice.
- Keep existing last-bag concurrency protection and whole-document cancellation rules.
- C4 diagrams must show roles, external offline stakeholders, protocols, responsibilities, synchronous/async paths and storage boundaries. Add focused Level 2 views, not fictional services or Level 3 component diagrams.

## Confirmed

- The user cancelled the real-estate domain and requested a new gardening-related sales project, described as “bán đất cây cảnh”.
- The user is unfamiliar with this business domain and wants the previous Matt Pocock grilling/domain-modeling workflow to guide discovery.
- Confirmed product scope: potting soil/growing media sold in bags. Ornamental plants are not part of the selected domain.
- Internal staff application only. No customer-facing storefront, shopping cart or customer self-service ordering.
- Purchase pre-packed bags from suppliers and resell them. No soil mixing, repacking or loose-weight sales in the initial scope.
- Q4–Q6 accepted on 16 October 2026: Admin/owner manages accounts, products and prices and sees all activity; Employees receive stock, create sales and query inventory.
- Products may have package variants (for example 5 kg and 10 kg). Each variant has its own SKU, purchase/sale price and integer bag stock. No kg/litre conversions. Description, brand and ingredients are optional.
- Sales workflow: draft sale → select SKUs/quantities → confirm → deduct stock. Store quantities and sale-price snapshots on the confirmed sale.
- Initial scope: in-store recording only, without shipping, credit/debt tracking or payment-provider integration.
- Q7–Q8 accepted: Employees draft purchase receipts containing Supplier, SKU, integer bag quantities and actual purchase unit prices; confirmation increases stock. Historical receipt prices are immutable snapshots and may vary between receipts.
- One stock location only. Batch/lot tracking and expiry dates are outside the initial scope.
- Confirmed sales cannot be edited or deleted. Admin may cancel the entire sale with a required reason; restore the original quantities exactly once and record cancelling actor/time.
- Partial returns and exchanges are outside the initial scope.
- Q9 accepted: Admin stock-adjustment slips add/remove integer bags with a required reason and history; no negative stock or direct stock-counter editing.
- Q10 accepted: independent gRPC service calculates line/order totals from quantities and unit prices; API invokes it for preview and confirmation. Redis Streams + Worker process post-mutation low-stock checks and internal notifications.
- Each SKU has a configurable low-stock threshold. Daily background summaries show effective sales revenue, bags sold and inventory snapshots.
- Stock mutations remain synchronous SQL transactions, not eventually consistent background deductions.
- Initial Q11 stack proposal was refined in the technology round below; PostgreSQL replaces SQL Server.
- Q12–Q14 accepted: daily backup plus an Admin manual trigger, retain 7 successful backups; no per-mutation backup requirement in the new domain.
- Admin may cancel an entire confirmed purchase receipt with a reason only when current stock is sufficient to reverse every received quantity; confirmation cancellation and stock reversal must be atomic and idempotent.
- Employees use current SKU sale prices, without per-sale overrides/discounts. Confirmed sale-price snapshots remain unchanged when the price list changes. Promotions, tax calculation and accounting profit are outside scope.

## Technology decisions — Q15–Q17

- Database: PostgreSQL, explicitly selected by the user. Use the Npgsql EF Core PostgreSQL provider and PostgreSQL-specific schema/concurrency mechanisms. SQL Server containers, T-SQL, `.bak` files and `rowversion` types from the old project do not apply.
- Frontend: React + Vite + TypeScript. Omit Ant Design. Start with HTML/CSS and small reusable React components rather than introducing an unrequested UI component library.
- Mapping: AutoMapper, explicitly requested by the user. Limit profiles to entity/DTO mapping; do not map request-supplied stock, role, totals or document status onto trusted persisted fields.
- AutoMapper package selection must document its current license/configuration requirements using the official documentation; no license key belongs in source control. No AutoMapper package version has been installed or pinned yet.
- Backend solution: layered REST API (Controllers → Services → Repositories), independent gRPC calculation service, Worker and necessary shared libraries.
- Persistence: EF Core migrations; export SQL scripts for submission.
- Messaging: Redis Streams. Stock confirmation remains synchronous PostgreSQL transactions; Worker handles low-stock notifications, daily summaries and backup requests.
- Deployment: Docker Compose. Documentation remains design work; no application has been implemented.
- Runtime major version remains an implementation choice: prefer a supported .NET/EF Core/provider combination; .NET 10 + EF Core 10 + Npgsql EF provider 10 is a verified compatible baseline. Do not mix mismatched EF/provider major versions.
- Backup design must use PostgreSQL tools (`pg_dump`/`pg_restore` with compatible versions), not SQL Server BACKUP DATABASE or `.bak` files.

## Concurrent sale requirement

The user asks how to handle two buyers competing for the last available item. In this internal system, interpret this as two staff members confirming sales for the last bag of the same SKU.

- Drafts do not reserve stock. Displayed availability is advisory until confirmation.
- Confirmation must conditionally deduct current stock inside the same database transaction that records the confirmed sale.
- With one bag left, one confirmation succeeds; the competing confirmation fails with an insufficient-stock conflict and does not confirm or deduct stock.
- For a sale containing multiple SKUs, all deductions and the sale confirmation succeed together or all roll back.
- Use a unique confirmation request identifier to make repeat submissions idempotent; retrying the same confirmation must not deduct stock twice.
- Do not rely solely on frontend checks, an in-process application lock or a prior standalone stock read.
- The precise SQL/EF mechanism will be specified with the new physical design. No stock reservation or distributed locking subsystem is needed for the agreed in-store scope.

## Confirmed business terms

- Product: a bagged-soil offering maintained in the shop catalogue with independently stocked package variants.
- Supplier: the business supplying pre-packed products to the shop.
- Staff: Admin/owner and Employee roles as described above.
- Stock: integer counts of pre-packed bags by SKU; confirmed receipts increase stock, confirmed sales decrease stock, and Admin sale cancellation restores stock.
- Continue satisfying the PRN232 PDF requirements while keeping assignment scope small.
- Replace the previous design only after the new scope is understood; preserve previous work as historical context.

## Confirmed first decision

The user selected option A: a shop selling pre-packed bagged soil for ornamental plants. Detailed catalogue fields remain to be specified. The previous real-estate backup policy and technical workflow are not automatically carried into the new domain.

## Documentation conventions retained

- English final documentation in `docs/`.
- C4 Level 1 and Level 2 only, plus conceptual ERD and Physical DB.
- Use c4-architecture and diagram-design; sources in `docs/diagrams/`, images in `docs/diagrams/images/`.
- Local Markdown issue tracking; repository publication remains with the user.

## Design status

The approved scope is now specified in [the system design](system-design.md), including the three enrichment workflows, C4 Level 1/2 views, conceptual ERD and physical database. Schema and SQL prototypes have been verified; the first runnable application increment and pending workflows are described in [implementation status](implementation.md). Earlier interview notes above record discovery history and are superseded by explicit decisions in that design where details were subsequently resolved.
