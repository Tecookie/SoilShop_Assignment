# Merge reviewed shop branches into main

Status: Complete

User corrected the target from dev to main. feature/advice-receipts-sales at eed7816 was already included; merge codex/complete-shop-workflows at 3411ed2 without rewriting history or pushing. Preserve main JWT configuration diagnostics, native setup docs, the existing Desktop stash and untracked SoilShop_OnTap folder. Resolve only generated static-check conflict by regeneration.

Verification: native solution build zero warnings/errors; frontend build passed; 47 net9 tests passed; EF model unchanged; full isolated Compose build and 12 operations/backup/restore checks passed. See docs/merge-main-checks.json. Stop only the named verification stack afterward and retain its test volumes.
