# PostgreSQL Backup and Restore

Policy: daily at or after 00:15 Asia/Ho_Chi_Minh plus Admin manual trigger; keep the latest seven successful archives total. No per-mutation backup. The application Worker implements this policy in a separate job loop. [Verification](verification.md) includes an isolated real archive restore and failed-backup retention check.

## Worker design

The Worker image installs PostgreSQL 16 client tools to match the server baseline. It connects using configured host/user/database and the environment-provided password; Compose mounts the separate `backup-data` volume at `/backups`. Unlike SQL Server server-side disk backups, **pg_dump runs in Worker and writes Worker's mounted path**.

Subprocesses use argument lists, not a shell command assembled from browser input. Filenames use the run UUID. The Worker writes a `.partial` file, checks process exit and `pg_restore --list`, then renames it as a complete archive. A database advisory lock serializes runs across Workers; a durable BackupRun identity prevents duplicate logical requests. The password is passed through the subprocess environment rather than CLI arguments.

```sh
# Examples use configured PGHOST, PGPORT, PGUSER and PGPASSFILE/PGPASSWORD.
# Replace the example path with a server-generated path under /backups.
pg_dump --dbname=potting_shop --format=custom --no-owner --no-acl --file=/backups/example.dump
pg_restore --list /backups/example.dump
```

Listing the archive is not a full recovery test. On success retain seven successful Daily/Manual files combined, delete older successful files and mark them not retained. On failure retain earlier good backups and record an error; bounded retries use a new temporary filename. File paths and raw dump contents are not exposed through Employee endpoints.

## Isolated restore drill

1. Save known expectations independently: SKU stock, sale/receipt states, fixed historical prices, movement count and one approved count.
2. Select a successful archive. Create a NEW database; do not point a Worker at it.

```sh
createdb potting_shop_restore_test
pg_restore --dbname=potting_shop_restore_test --no-owner --no-acl --exit-on-error /backups/example.dump
psql --dbname=potting_shop_restore_test --command="SELECT code,stock_qty,stock_version FROM skus ORDER BY code;"
psql --dbname=potting_shop_restore_test --command="SELECT count(*) FROM stock_movements;"
```

3. Compare restored quantities, snapshots and FKs to the saved expectations. A normal dump includes database objects/data, not cluster roles; provision destination roles/configuration separately.
4. Do not auto-start consumers against restored queued/outbox records: old operations can replay. A later real recovery requires stopping writers, reconciling nonterminal operations and credentials, then explicitly switching configuration. That procedure is manual and not an assignment UI feature.

Use only trusted internally generated archives for this drill. Local backups share the host's failure domain. No WAL/PITR or offsite backup is promised. Official references: [pg_dump](https://www.postgresql.org/docs/current/app-pgdump.html), [pg_restore](https://www.postgresql.org/docs/current/app-pgrestore.html).
