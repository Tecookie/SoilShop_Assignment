# Diagram Sources and Exports

The gallery is [index.html](index.html). Every figure has a self-contained HTML source plus PNG (2560×1440) and SVG exports in `images/`.

| Figure | Model |
|---|---|
| c4-l1-context | C4 System Context |
| c4-l2-containers | C4 Container |
| erd-conceptual | Business entities/cardinality |
| db-catalogue | Developer / Project / Building |
| db-unit-location | Unit → Project / Building |
| db-unit-audit | Unit details, history and actors |
| db-batches | Batch request and Unit origin |
| db-summaries | Project snapshot and SummaryRun |
| db-operations | Outbox and BackupRun |

The [Structurizr workspace](workspace.dsl) contains only C4 Context/Container views. HTML figures are designed with diagram-design, independently of Structurizr's automatic layout. Staff roles are combined into one Staff box on the container figure; the DSL keeps their individual relationships. Offline developer-to-employee information is explicit on L1, not an API integration. Physical figures deliberately omit non-key columns and cross-subsystem edges where their captions disclose them; the full [SQL](../database/schema.sql) and [dictionary](../database/data-dictionary.md) are authoritative.

## Regeneration

1. Run `python docs/tools/generate_design_assets.py` from the repository root.
2. Export each HTML figure with the installed diagram-design helper: `python <skill-directory>/scripts/export_svg.py <source.html> <images/output.svg> --system-fonts`.
3. Run `node docs/tools/render_design_assets.cjs` with Playwright available. The script defaults to installed Chrome; set `DESIGN_BROWSER_CHANNEL=msedge` for Edge. If Node resolves no local Playwright module, set `CODEX_DESIGN_NODE_MODULES` to the bundled Node module directory returned by Codex workspace dependencies.
4. Run diagram-design's `scripts/self_check.py` for each figure and inspect the rendered images.

Project-local colors and system-font choices are recorded in [style-guide.md](style-guide.md). No global skill profile is modified.
