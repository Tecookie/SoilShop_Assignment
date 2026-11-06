# Target .NET 9

Status: Complete

User requested .NET 9. Retarget all projects, SDK/tool manifest, Microsoft/EF/Npgsql packages and Docker images. Preserve applied migration IDs and database contents; regenerate the idempotent SQL with EF9. Keep AutoMapper, gRPC and Redis features. Update current setup/design docs. Verify native build/tests, EF model consistency and running Compose/browser workflows before committing. Existing untracked launch settings belong to the user and are excluded from this change.

## Verification

Native build has zero warnings/errors; 20 net9.0 tests pass. EF9 reports no model delta. Compose API/calculation/Worker report runtime 9.0.22; existing stock/document fingerprint matches. Five advice, six seed and nine trading browser checks pass. Export and current docs updated; see docs/net9-checks.json.
