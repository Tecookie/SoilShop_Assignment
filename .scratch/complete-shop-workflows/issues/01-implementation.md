# Implement and verify remaining workflows

Status: Complete

See ../spec.md. Work spans Inventory, Reporting, Backups and Administration owners;
keep existing HTTP contracts compatible and preserve all existing volumes/history.

## Comments

- Baseline main b71b1f4; working branch codex/complete-shop-workflows.
- .NET integration tests, browser checks, isolated Redis/Worker recovery, seven-backup retention, backup failure and isolated PostgreSQL restore passed. See `docs/verification.md`.
