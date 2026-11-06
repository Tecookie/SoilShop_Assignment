# Source-backed soil metadata and product matching

This assignment supports counter staff choosing among sealed, pre-packed bags. A result is a match to entered needs, not a universal best soil or a prediction of plant health.

## At a glance — section 5.2

| Topic | Behavior |
| --- | --- |
| Actors | Admin maintains/reviews metadata; authenticated staff request advice |
| Eligible SKU | Active, in stock, with active Product and source-reviewed metadata |
| Calculation | REST API → independent gRPC `RecommendProducts` → rule `advice-v2` |
| Result | Up to three SKUs with score, reasons, missing data, source and reviewed version |
| Next action | Create a one-bag sale Draft; no stock reservation |
| Full trading flow | [Receipts, quotes, sales and cancellation](trading-workflows.md) |

## Coverage of the nine factors

| Factor | Stored metadata | Search behavior |
| --- | --- | --- |
| Drainage | Low / Medium / High, nullable | Exact selected level, 3 points |
| Aeration | Low / Medium / High, nullable | Exact selected level, 3 points |
| Moisture retention | Low / Medium / High, nullable | Exact selected level, 3 points |
| Structure and durability | Source-backed notes | Require information to be present |
| pH | Published minimum/maximum | Entire published interval must fit requested interval, 2 points |
| Nutrition | Label/NPK notes | Require information to be present |
| EC | Published interval in mS/cm plus measurement method | Maximum must fit requested limit with same method, 2 points |
| Organic matter and biology | Separate notes | Separate information-presence filters |
| Cleanliness/treatment | Supplier/source claim notes | Require information to be present; no certification inferred |

Before this increment, drainage, retention, pH, ingredients and source/review columns existed, but there was no complete metadata editor or ranking workflow. Aeration, structure, nutrition, EC, organic/biology, cleanliness, planting context and sample availability are now supported. Unknown remains NULL, distinct from Low, zero or false.

Plant groups are multi-use source-backed tags: general, foliage, flowering, succulent/cactus, orchid, seedling, bonsai and acid-loving. They are distinct from the product's primary catalogue type. Tags earn 3 points. Pot / in-ground / both context earns 1 point; Both satisfies either location. A sample-available requirement is a hard filter, not a score. The application does not manufacture pH/EC targets from plant names.

## Staff workflow

1. Admin opens **Catalogue → Thông số đất**. Record information from a product label, supplier document or attributable test. Leave unavailable fields blank. Include a source reference.
2. Save, inspect the stored values, then explicitly choose **Duyệt thông tin theo nguồn**. Unsaved edits disable review. This records the Admin and timestamp for that version; it does not certify laboratory quality.
3. Employee opens **Tìm đất phù hợp**, selects needs and optional budget/packaging constraints. A budget alone is insufficient: at least one plant, physical, pH, EC or location preference is required.
4. Read up to three results, their reasons, missing information, source, reviewer and version. Open the details to see all nine factors.
5. Choose **Tạo phiếu bán nháp · 1 bao**. Review/confirm in Sales. Advice creates no reservation; confirmation rechecks authoritative price and inventory.

All notes are descriptive text. Requiring nutrition/cleanliness notes checks presence, not nutrient sufficiency or pathogen absence. Staff may record supplier sample availability; no sample-order workflow is included.

## Review and matching rules

### Permissions and review

Metadata writes and review are Admin-only. Authenticated staff may read metadata and request advice. An optimistic expected product version prevents lost updates; saving metadata or changing master product data clears review. Product/tag changes commit atomically. AutoMapper maps read DTOs; Service controls review, version and other trusted fields.

### API and gRPC boundary

API loads active, in-stock, source-reviewed candidates in a RepeatableRead database snapshot, applies per-bag budget and packaging filters and completes the transaction before calling gRPC.

- More than 500 eligible candidates requires narrower filters; no silent truncation.
- The independent calculation container has no database access.
- `RecommendProducts` uses rule **advice-v2**; `CalculateTotals` calculates transaction totals.
- An unavailable RPC returns `503`; there is no hidden local ranking fallback.

### Matching decisions

| Condition | Outcome |
| --- | --- |
| Known value conflicts with a selected criterion | Exclude candidate |
| Selected value is unknown; default mode | Exclude candidate |
| Selected value is unknown; **Allow incomplete** enabled | Show missing criterion explicitly; award zero points for it |
| EC method missing or different | Incomparable; never count as a numerical match |
| EC method is `Other` | May be stored; cannot be requested for comparison |
| No positive match | Do not return candidate |

Sort by **score descending → price ascending → SKU ID ascending**. The maximum score includes only selected scoring criteria; it is not a quality percentage.

### Measurement validation

pH accepts paired bounds 0–14 with two decimals. EC accepts paired nonnegative bounds with three decimals and an SME, Extract1to2, PourThrough or Other method; the unit is mS/cm. Save/review responses include current version. Notes are length-limited; input allowlists and DTO validation reject unsupported values.

## APIs and verification

| Method | Endpoint | Permission / input |
| --- | --- | --- |
| GET | `/api/products/{id}/metadata` | Authenticated staff |
| PUT | `/api/products/{id}/metadata` | Admin; full replacement with `expectedVersion` |
| POST | `/api/products/{id}/metadata/review` | Admin; `expectedVersion` |
| POST | `/api/product-advice` | Authenticated staff; selected preferences |

Run `./scripts/test.ps1` for PostgreSQL integration and rule tests. With Compose running and local `.env` credentials, run `scripts/browser_advice_check.py` using Python with Playwright/Chrome. It creates clearly labelled fictional fixtures, receives stock through a confirmed receipt, enters/reviews metadata through React, calls real gRPC, checks a positive result and creates a sale draft. It tests role restrictions, budget, EC mismatch/incomplete handling and review invalidation. It clears the fixture's review afterward so simulated measurements are not eligible for later advice. History remains inspectable. Existing seed products are not silently assigned or reviewed scientific values.

### Ready-to-search demo

Run `./scripts/seed-demo.ps1 -WithAdvice` (or add `-SkipBuild` when current images are built). This explicitly imports six separate `[mẫu tư vấn]` products, one SKU per group, and receives eight bags each through a confirmed receipt. Codes are `ADVICE-V1-P01` through `P06`; General is P01. Their fictional label supplies only a plant-use tag. Review records the seed Admin for that synthetic classification, not a real horticultural finding. All physical/scientific measurements remain unknown. Select only **Nhóm cây → Đa dụng** then **Tìm sản phẩm** for the simplest positive demo. Strict extra constraints can exclude unknown measurements; opt-in incomplete mode shows them explicitly. Bonsai and acid-loving groups need separately supplied metadata.

Rerunning preserves metadata edits/review invalidation and never receives the same stock twice. Baseline `SEED-V1` products and staff-created products are not modified. A primary catalogue type never automatically establishes a source-reviewed advisory plant tag. The empty state explains this difference and the Admin save/review steps.

## Horticultural rationale and limitations

Physical properties of container media must be balanced for roots and watering practice; a darker appearance or more fertilizer does not establish suitability. See [Rutgers: Growing Media for Container Production](https://extension.rutgers.edu/fs812) and [NC State: Plants Grown in Containers](https://content.ces.ncsu.edu/extension-gardener-handbook/18-plants-grown-in-containers).

EC interpretation depends on extraction method. See [UMass: Methods of Greenhouse Media Testing](https://www.umass.edu/agriculture-food-environment/greenhouse-floriculture/fact-sheets/methods-of-greenhouse-media-testing-how-they-differ) and [UMass: Soluble Salts and EC](https://www.umass.edu/agriculture-food-environment/greenhouse-floriculture/fact-sheets/soluble-salts-electrical-conductivity-ec-for-greenhouse-crops). These references justify measurement context; their generic thresholds are not copied into fictional product data. Climate, watering, pot geometry and actual root health remain staff/customer considerations outside this simple rules engine. No soil mixing, machine learning, certification or laboratory integration is added.
