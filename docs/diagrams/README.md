# Potting Soil Shop — Diagrams

Open the [gallery](index.html). All figures have editable self-contained HTML sources, offline SVG exports and PNG exports at 2× in `images/`. Canvas dimensions are recorded per figure in `manifest.json`.

| Source | Meaning |
|---|---|
| [c4-l1-context.html](c4-l1-context.html) | Admin/Employee access and offline Supplier/Customer context. |
| [c4-l2-containers.html](c4-l2-containers.html) | All six executing services plus backup store, responsibilities, protocol links and application boundary. |
| [c4-l2-synchronous.html](c4-l2-synchronous.html) | Same Level 2 containers, focused on advice/quotes and atomic stock confirmation. |
| [c4-l2-asynchronous.html](c4-l2-asynchronous.html) | Same Level 2 containers, focused on outbox producer, broker, Worker, reports and archives. |
| [erd-trading.html](erd-trading.html) | Conceptual product/SKU/receipt/sale relationships. |
| [erd-operations.html](erd-operations.html) | Conceptual count/adjustment/movement/replenishment and metadata relationships. |

Physical database figures are `db-catalogue`, `db-purchases`, `db-sales`, `db-inventory`, `db-replenishment`, `db-tags` and `db-background`. They show selected columns/FKs; captions disclose omitted cross-subsystem references and the dictionary contains the full schema.

## C4 fidelity

The [Structurizr workspace](workspace.dsl) has exactly one System Context view and three Container perspectives. There are no component/deployment/code views. The synchronous HTML view combines Admin/Employee into one People box for readability; the DSL preserves separate actor nodes. All container/store identities are shared across C2 views. Responsibilities listed inside a container are descriptive text, not extra C3 nodes.

Offline supplier/customer interactions are human context, not software integrations. API hosts the outbox publisher; Worker consumes Redis and writes backups. PostgreSQL does not write Worker archive paths. gRPC has no database access. Return values are implicit on request arrows and described inside nodes/captions.

The HTML/SVG layout is authored with diagram-design; the canonical C4 model is in Structurizr DSL. Both express the same boundaries. Docker Structurizr validation succeeded; visual layout is verified from the HTML exports rather than generated PlantUML.

## Regenerate

1. `python docs/tools/generate_design_assets.py` generates schema/dictionary and HTML/manifest from the approved models.
2. Export each figure using the installed diagram-design `scripts/export_svg.py <source.html> <images/output.svg> --system-fonts` helper.
3. `node docs/tools/render_design_assets.cjs` renders with installed Chrome and Playwright. Set `CODEX_DESIGN_NODE_MODULES` if using bundled Node modules; set `DESIGN_BROWSER_CHANNEL=msedge` to use Edge.
4. Run `python docs/tools/validate_design_assets.py` and diagram-design `scripts/self_check.py` for all figure sources. Inspect the PNG files after changing geometry.
5. `python docs/tools/verify_postgres_design.py` runs the isolated database checks using an existing `postgres:16-alpine` image, then removes only its own temporary container/volume.

Project colors remain white/navy/cyan from the approved custom style; no global skill profile is changed. System fonts are intentional for portable offline rendering.
