# Backup and Restore Design

This is an implementation guide and demo plan. These commands have not been executed against an application database in this design-only repository.

## Triggers and retention

- Daily full backup at 00:15 Vietnam time.
- One background backup request per successfully committed business mutation, including a whole bulk operation as one mutation.
- Admin manual trigger. Backup/summary/job bookkeeping does not create change events.
- Serial processing, visible Queued/Processing/Completed/Failed results and bounded retries.
- Keep 7 verified Daily files and 7 verified Change/Manual files; prune only after new success.
- Backups are SQL state at execution time, not exact snapshots of each intermediate edit. SQL credentials and password hashes are sensitive parts of the database copy.

## Volume and permissions

Mount a named volume such as `catalogue_backups` at `/var/opt/mssql/backup` in both SQL Server and Worker. SQL Server writes backups to its own container filesystem, not the Worker's working directory. Worker needs filesystem permission to prune retained files; configure ownership/group access explicitly. Keep `catalogue_sql_data` and `catalogue_redis_data` separate. Do not remove named volumes during normal stop/restart.

Normal API credentials should only access the application schema. Use a separate Worker backup connection with the required `BACKUP DATABASE` permission. Restore/verification can require stronger permissions; provision an explicit local administrative connection for the restore drill. Keep its secret out of source control. [Microsoft's Linux guide](https://learn.microsoft.com/en-us/sql/linux/business-continuity/backup-restore/database-backup-restore?view=sql-server-ver17) describes server-side paths and backup/restore execution.

## SQL templates

The Worker generates the filename from UTC time, trigger kind, event ID and attempt ID. Accept no path or database name from the browser. Use a fixed database identifier and validated internal paths, with parameterized values where supported.

```sql
BACKUP DATABASE [BrokerCatalogue]
TO DISK = N'/var/opt/mssql/backup/catalogue-change-example.bak'
WITH COPY_ONLY, CHECKSUM, INIT, STATS = 10;

RESTORE VERIFYONLY
FROM DISK = N'/var/opt/mssql/backup/catalogue-change-example.bak'
WITH CHECKSUM;
```

`INIT` is safe here only because each attempt gets a unique filename. Full [copy-only backups](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/copy-only-backups-sql-server?view=sql-server-ver17) avoid changing a differential backup base. No differential or transaction-log backup chain is planned. [VERIFYONLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver17) checks readability/completeness but does not prove restored application data is correct.

## Restore drill (isolated destination)

1. Prepare known data: record a Project, several codes, one price and one status change. Run a manual backup and save the filename independently of the source DB.
2. Connect to SQL Server's `master` database with an authorized admin account. Inspect the file list:

```sql
RESTORE FILELISTONLY
FROM DISK = N'/var/opt/mssql/backup/catalogue-change-example.bak';
```

3. Substitute the logical file names returned by FILELISTONLY. Restore into a NEW database and distinct physical files, leaving the original database untouched:

```sql
-- Replace logical names below with the actual FILELISTONLY output.
RESTORE DATABASE [BrokerCatalogue_RestoreTest]
FROM DISK = N'/var/opt/mssql/backup/catalogue-change-example.bak'
WITH MOVE N'BrokerCatalogue' TO N'/var/opt/mssql/data/BrokerCatalogue_RestoreTest.mdf',
     MOVE N'BrokerCatalogue_log' TO N'/var/opt/mssql/data/BrokerCatalogue_RestoreTest_log.ldf',
     RECOVERY, CHECKSUM, STATS = 10;

DBCC CHECKDB (N'BrokerCatalogue_RestoreTest') WITH NO_INFOMSGS;
SELECT COUNT(*) AS UnitCount FROM BrokerCatalogue_RestoreTest.dbo.Units;
SELECT Code, PriceVnd, ManagementStatus
FROM BrokerCatalogue_RestoreTest.dbo.Units ORDER BY Code;
SELECT * FROM BrokerCatalogue_RestoreTest.dbo.UnitStatusHistory ORDER BY Id;
```

4. Compare restored records to the saved expectation. Check nullable area values, extra attributes, account roles and history. Do not attach the normal Worker to the restored database: old outbox/Queued job state could replay and alter it.
5. If later choosing to recover the live application, stop mutating services, select the restored database explicitly, reconcile all nonterminal operations/leases and outbox entries, and ensure restored user tokens cannot grant stale access. This is a manual recovery procedure, not an assignment UI feature.

## Failure behavior

A failed backup leaves the originating business change committed and a visible failed Run. Retry with the same EventId and a new unique file; repeated Redis delivery must not create a new logical request. The completion record is written after the file, so a restored copy may contain an in-progress Run. An interrupted backup's partial/orphan file is not counted as a successful retained backup. Local volumes do not survive loss of the entire host; cloud replication is outside scope.
