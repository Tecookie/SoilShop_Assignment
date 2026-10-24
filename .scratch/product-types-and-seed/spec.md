# Product classification and repeatable demo data

Status: Complete

Add one primary Product Type to each Product: GeneralPurpose, FoliageMix, FloweringMix, SucculentMix, OrchidMix or SeedStartingMix. Existing/unknown products remain Unclassified. These are shop catalogue groups based on package descriptions, not certified plant advice; keep multi-use tags separate. Expose validated type selection/filtering in catalogue and inventory.

Seed 8 fictional labelled products, 16 package SKUs, 3 suppliers, receipt/sale examples and stock states (zero, last bag, low, normal and inactive). Stock must arise from normal confirmation transactions, with ledger/outbox. Seed is an explicit CLI operation, repeatable without adding stock again or resetting staff edits, and resumable after partial interruption. Preserve all existing shop data. Include an Employee only if configured with a generated local password.

Acceptance: migration and exported SQL; invalid types rejected; filter results correct; seeding twice has identical stock/document/ledger counts; post-seed staff edits/sales survive rerun; existing browser workflow continues to work.

## Verification

2026-10-24: 14 application tests pass; six read-only seeded UI checks and the original nine-check receive/sell browser workflow pass against Compose. Explicit CLI import applied to local shop; second import reported AlreadyApplied and preserved stock signature and document/movement/outbox counts. Disk-space/Docker interruption was recovered without deleting shop data. See docs/demo-data.md and docs/seed-checks.json.
