# Demo catalogue and seed

Run `./scripts/seed-demo.ps1` from PowerShell 7 with Docker Desktop running. The script builds/starts Compose, generates missing Employee credentials in ignored `.env`, then runs an explicit one-shot API process to import demo data. There is no public seed endpoint and ordinary API startup does not seed automatically. Use `-SkipBuild` only when the current images have already been built.

## Product classification

A Product has one primary `productType`; its SKUs inherit that group. Packaging amount/unit, price and bag stock belong to each SKU. Catalogue and inventory support filtering by type.

| Code | Catalogue group | Vietnamese meaning |
|---|---|---|
| GeneralPurpose | General-purpose mix | Đất trồng đa dụng |
| FoliageMix | Foliage mix | Đất cho cây kiểng lá |
| FloweringMix | Flowering mix | Đất cho cây hoa |
| SucculentMix | Succulent / cactus mix | Đất cho sen đá, xương rồng |
| OrchidMix | Orchid mix | Giá thể cho lan |
| SeedStartingMix | Seed-starting mix | Đất/giá thể ươm cây |
| Unclassified | Unclassified | Chưa có thông tin phân loại |

These are practical shop groups for pre-packed offerings. They are not an exhaustive botanical classification or a guarantee of plant suitability. Existing products become Unclassified when the additive migration runs; their original information is preserved. Multi-use tags and source-reviewed advice remain separate planned workflows. The application does not introduce mixing, repacking, loose sales, pots or tools.

## Seed contents

All product/supplier names and prices below are fictional demonstration data. Names include `[demo]`, codes begin `SEED-V1-`, and sources explicitly say they are fictional. No laboratory measurements, pH values or advisory review/certification are invented. Drainage, retention and pH remain unknown; the data is not marked reviewed.

| Product code | Product | Type | Packages | Unit prices (VND) | Bags after seed |
|---|---|---|---|---|---|
| SEED-V1-P01 | Everyday Potting Mix | GeneralPurpose | 5 kg / 10 kg | 55,000 / 110,000 | 0 / 12 |
| SEED-V1-P02 | Leafy Plant Mix | FoliageMix | 5 L / 20 L | 65,000 / 130,000 | 1 / 27 |
| SEED-V1-P03 | Flowering Plant Mix | FloweringMix | 5 kg / 10 kg | 60,000 / 120,000 | 4 / 10 |
| SEED-V1-P04 | Succulent & Cactus Mix | SucculentMix | 2 L / 5 L | 35,000 / 70,000 | 0 / 10 |
| SEED-V1-P05 | Orchid Potting Medium | OrchidMix | 5 L / 10 L | 70,000 / 140,000 | 3 / 16 |
| SEED-V1-P06 | Seed-starting Mix | SeedStartingMix | 2 L / 5 L | 30,000 / 60,000 | 8 / 24 |
| SEED-V1-P07 | Lightweight Potting Mix | GeneralPurpose | 10 L / 20 L | 90,000 / 180,000 | 12 / 30 |
| SEED-V1-P08 | Indoor Foliage Mix | FoliageMix | 5 L / 10 L | 75,000 / 150,000 | 6 / 10 (last SKU inactive) |

The import adds **8 Products, 16 SKUs, 3 Suppliers, 2 confirmed + 1 draft Purchase Receipts, and 3 confirmed + 1 draft Sales**. It also creates an Employee if configured. Admin already comes from bootstrap. Read Admin and Employee credentials from `.env`; there are no committed login passwords. Existing accounts/passwords are preserved.

Current stock comes from receipt/sale confirmations through the normal Service, gRPC totals, transactional stock updates, movements and outbox. The two draft documents have no stock effect. Worker creates low-stock suggestions for active low/zero-stock SKUs; one-bag stock supports the last-bag concurrency demonstration. No kg/L conversion is performed.

## Repeatability and recovery

- Deterministic codes identify seed records and stable action keys identify confirmations. Already existing records are not overwritten.
- A PostgreSQL session advisory lock serializes seed processes. Network gRPC requests still happen outside stock row-lock transactions.
- If interrupted before completion, rerunning resumes saved steps rather than duplicating receipts or deducting stock again.
- The final confirmed sale is the completion marker. Once complete, rerunning performs no data reset, even after staff price edits, additional sales, draft confirmation or future cancellation. Do not delete the marker to attempt a reset.
- Reseeding preserves all pre-existing records, including earlier browser-smoke records. Counts above refer specifically to `SEED-V1-` records, not the total database contents.
- No seed runs for stock-count approval, recommendations, backups or summaries are fabricated; those application workflows remain pending.

Integration tests cover interrupted import recovery, counts/types, preserved unknown measurements, duplicate import and post-seed staff changes. Application verification is described in [verification.md](verification.md).

`python scripts/browser_seed_check.py` checks the seeded catalogue and both roles without changing stock; the original `scripts/browser_smoke.py` adds separately labelled workflow-demo records. The seed itself was applied and replay-verified on 24 October 2026.
