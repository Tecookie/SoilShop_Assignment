# Vietnamese demo product names

Status: Complete

Translate seeded product names and displayed product-type labels into Vietnamese. Apply a guarded data migration to existing English fixture names, preserving staff-customized names, codes, price, stock and transaction history. Update future seed/browser fixtures and verify the running catalogue.

## Verification

2026-10-24: Existing fixture names translated in the running PostgreSQL database. SKU identity/stock/version/price hash unchanged. Docker build and six read-only browser checks pass; all six category labels and the seed edit form show Vietnamese. English documentation retained with Vietnamese product-name examples.
