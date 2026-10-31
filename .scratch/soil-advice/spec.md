# Source-backed soil metadata and product matching

Status: Complete

Extend the approved advice feature with the user's nine soil factors. Keep the assignment scope: packaged soil, no mixing/AI/botanical certification. Store nullable qualitative drainage/aeration/retention, pH interval, EC interval in mS/cm with extraction method, structure/nutrition/organic/biology/cleanliness notes, planting context and sample availability. Admin edits metadata, supplies a source and separately reviews the current version. Changes clear review in the same transaction. Multi-use plant tags are distinct from primary product type.

Employee/Admin can filter and request up to three available SKU matches. API applies current active/in-stock/package/per-bag-budget/review eligibility and calls the existing independent gRPC host. Rule advice-v2 weights each selected physical match 3, plant group 3, pH/EC 2 each, context 1. Scores mean preference matches, not scientific quality or success probability. Known conflicts exclude; selected unknowns exclude by default, or remain explicitly incomplete when staff opt in. EC requires a matching measurement method; no cross-method conversion. Notes-presence filters do not assert cleanliness or nutrient adequacy. Require a meaningful preference and return no results for zero matches. Preserve source/review/version/time/reasons/missing data with results; advice never reserves stock.

Implement nullable additive migration, metadata read/edit/review API and UI, advice gRPC/API/UI, tests and updated English docs/schema/diagrams. Preserve stock, prices, existing fixtures and their unknown metadata. Generic university guidance is design context only and never supplies invented SKU measurements or plant thresholds. Staff enter desired pH/EC ranges; no automatic botanical numeric presets.

## Completion evidence

20 application tests, five real-stack advice browser checks, six seed and nine trading regressions passed. See `docs/soil-advice.md`, `docs/verification.md` and `docs/soil-advice-checks.json`.
