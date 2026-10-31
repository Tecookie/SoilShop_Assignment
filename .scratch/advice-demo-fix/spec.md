# Advice empty for General

Status: Complete

Database evidence: no products have reviewed metadata; SEED-V1 products have primary product types but no advisory plant tags. Ranking is working as specified, but normal demo import has no positive advice fixture. Add an explicit -WithAdvice import with separate clearly fictional source-reviewed classification-only fixtures (six groups); never invent physical measurements or review staff products. Preserve stock through confirmed receipts and repeated imports. Improve the field hint/empty message with actionable review steps. Verify General in the real browser and add regression coverage for repeat imports and edit invalidation.

## Evidence

21 application tests pass. Explicit live import and rerun preserve stock; four General and five existing advice browser checks pass. Pristine-seed browser stock assertions are not applicable to existing staff-modified balances and were not reset or counted as passing; details in docs/verification.md.
