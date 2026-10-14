# Broker Catalogue Diagram Style

Custom project-local tokens approved during requirements discovery; no global skill styling is overwritten.

| Semantic role | Value |
|---|---|
| paper | #FFFFFF |
| paper-2 | #F1F5F9 |
| ink | #142B49 |
| muted / soft / rule-solid | #48617C |
| rule | #D5DFEA, decorative |
| accent | #007C91 |
| accent-tint | #E8F6FA |
| link | #175E9B |

Font source: system (offline portable). Names: Segoe UI / Arial; technical labels: Consolas / monospace. No network font requests. This is a project-specific typography choice; images are rendered with the available Windows fonts.

Canvas: 1280×720, exported PNG at 2×. Architecture uses the presentation ramp; Physical DB uses the documented column-row sizes. SVGs preserve accessible titles/descriptions. Sources are self-contained HTML; SVG/PNG are generated exports.

Architecture and conceptual diagrams retain all modeled relationships. Physical diagrams show at most seven columns per table plus an explicit overflow count; all columns/constraints are in the data dictionary. Outbox EventId correspondence is not drawn as a database FK because no such SQL constraint exists.
