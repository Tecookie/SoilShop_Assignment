# Physical Database Data Dictionary

SQL Server baseline: `BrokerCatalogue`. Generated from [schema.sql](schema.sql). All foreign keys use NO ACTION unless explicitly marked CASCADE. Operational EventId/OperationId references are enforced by handlers, not polymorphic SQL foreign keys.

Unknown descriptive values use NULL. UTC timestamps use datetime2(3); rowversion is a concurrency token, not a date. The Services reject fractional VND before SQL conversion.

## Users

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `int` | No | `Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY` |
| Username | `nvarchar(100)` | No | `Username nvarchar(100) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE` |
| DisplayName | `nvarchar(150)` | No | `DisplayName nvarchar(150) NOT NULL` |
| PasswordHash | `nvarchar(500)` | No | `PasswordHash nvarchar(500) NOT NULL` |
| Role | `varchar(16)` | No | `Role varchar(16) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin','Employee'))` |
| IsActive | `bit` | No | `IsActive bit NOT NULL CONSTRAINT DF_Users_Active DEFAULT 1` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Users_Created DEFAULT SYSUTCDATETIME()` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username nvarchar(100) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    DisplayName nvarchar(150) NOT NULL,
    PasswordHash nvarchar(500) NOT NULL,
    Role varchar(16) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin','Employee')),
    IsActive bit NOT NULL CONSTRAINT DF_Users_Active DEFAULT 1,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Users_Created DEFAULT SYSUTCDATETIME(),
    Version rowversion NOT NULL
```

## Developers

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `int` | No | `Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Developers PRIMARY KEY` |
| Code | `nvarchar(50)` | No | `Code nvarchar(50) NOT NULL CONSTRAINT UQ_Developers_Code UNIQUE` |
| Name | `nvarchar(200)` | No | `Name nvarchar(200) NOT NULL` |
| ContactName | `nvarchar(150)` | Yes | `ContactName nvarchar(150) NULL` |
| Email | `nvarchar(254)` | Yes | `Email nvarchar(254) NULL` |
| Phone | `nvarchar(30)` | Yes | `Phone nvarchar(30) NULL` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Developers_Created DEFAULT SYSUTCDATETIME()` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Developers PRIMARY KEY,
    Code nvarchar(50) NOT NULL CONSTRAINT UQ_Developers_Code UNIQUE,
    Name nvarchar(200) NOT NULL,
    ContactName nvarchar(150) NULL,
    Email nvarchar(254) NULL,
    Phone nvarchar(30) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Developers_Created DEFAULT SYSUTCDATETIME(),
    Version rowversion NOT NULL
```

## Projects

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `int` | No | `Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Projects PRIMARY KEY` |
| DeveloperId | `int` | No | `DeveloperId int NOT NULL` |
| Code | `nvarchar(50)` | No | `Code nvarchar(50) NOT NULL CONSTRAINT UQ_Projects_Code UNIQUE` |
| Name | `nvarchar(200)` | No | `Name nvarchar(200) NOT NULL` |
| Address | `nvarchar(500)` | Yes | `Address nvarchar(500) NULL` |
| Description | `nvarchar(2000)` | Yes | `Description nvarchar(2000) NULL` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Projects_Created DEFAULT SYSUTCDATETIME()` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Projects PRIMARY KEY,
    DeveloperId int NOT NULL,
    Code nvarchar(50) NOT NULL CONSTRAINT UQ_Projects_Code UNIQUE,
    Name nvarchar(200) NOT NULL,
    Address nvarchar(500) NULL,
    Description nvarchar(2000) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Projects_Created DEFAULT SYSUTCDATETIME(),
    Version rowversion NOT NULL,
    CONSTRAINT FK_Projects_Developer FOREIGN KEY (DeveloperId) REFERENCES dbo.Developers(Id)
```

## Buildings

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `int` | No | `Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Buildings PRIMARY KEY` |
| ProjectId | `int` | No | `ProjectId int NOT NULL` |
| Code | `nvarchar(40)` | No | `Code nvarchar(40) NOT NULL` |
| Name | `nvarchar(200)` | No | `Name nvarchar(200) NOT NULL` |
| FloorCount | `smallint` | No | `FloorCount smallint NOT NULL CONSTRAINT CK_Buildings_Floors CHECK (FloorCount BETWEEN 1 AND 99)` |
| Classification | `varchar(16)` | Yes | `Classification varchar(16) NULL CONSTRAINT CK_Buildings_Class CHECK (Classification IN ('HighRise','LowRise'))` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Buildings_Created DEFAULT SYSUTCDATETIME()` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Buildings PRIMARY KEY,
    ProjectId int NOT NULL,
    Code nvarchar(40) NOT NULL,
    Name nvarchar(200) NOT NULL,
    FloorCount smallint NOT NULL CONSTRAINT CK_Buildings_Floors CHECK (FloorCount BETWEEN 1 AND 99),
    Classification varchar(16) NULL CONSTRAINT CK_Buildings_Class CHECK (Classification IN ('HighRise','LowRise')),
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Buildings_Created DEFAULT SYSUTCDATETIME(),
    Version rowversion NOT NULL,
    CONSTRAINT UQ_Buildings_ProjectCode UNIQUE (ProjectId,Code),
    CONSTRAINT UQ_Buildings_ProjectId UNIQUE (ProjectId,Id),
    CONSTRAINT FK_Buildings_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id)
```

## UnitBatches

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `uniqueidentifier` | No | `Id uniqueidentifier NOT NULL CONSTRAINT PK_UnitBatches PRIMARY KEY` |
| RequestId | `uniqueidentifier` | No | `RequestId uniqueidentifier NOT NULL CONSTRAINT UQ_UnitBatches_Request UNIQUE` |
| RequestedByUserId | `int` | No | `RequestedByUserId int NOT NULL` |
| ProjectId | `int` | No | `ProjectId int NOT NULL` |
| BuildingId | `int` | Yes | `BuildingId int NULL` |
| UnitType | `varchar(16)` | No | `UnitType varchar(16) NOT NULL CONSTRAINT CK_UnitBatches_Type CHECK (UnitType IN ('Apartment','Townhouse','Villa'))` |
| PayloadJson | `nvarchar(max)` | No | `PayloadJson nvarchar(max) NOT NULL CONSTRAINT CK_UnitBatches_Json CHECK (ISJSON(PayloadJson)=1)` |
| RequestedCount | `int` | No | `RequestedCount int NOT NULL CONSTRAINT CK_UnitBatches_Count CHECK (RequestedCount BETWEEN 1 AND 500)` |
| CreatedCount | `int` | No | `CreatedCount int NOT NULL CONSTRAINT DF_UnitBatches_CreatedCount DEFAULT 0` |
| Status | `varchar(16)` | No | `Status varchar(16) NOT NULL CONSTRAINT DF_UnitBatches_Status DEFAULT 'Queued'` |
| Attempts | `int` | No | `Attempts int NOT NULL CONSTRAINT DF_UnitBatches_Attempts DEFAULT 0` |
| ErrorMessage | `nvarchar(2000)` | Yes | `ErrorMessage nvarchar(2000) NULL` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UnitBatches_Created DEFAULT SYSUTCDATETIME()` |
| StartedAtUtc | `datetime2(3)` | Yes | `StartedAtUtc datetime2(3) NULL` |
| CompletedAtUtc | `datetime2(3)` | Yes | `CompletedAtUtc datetime2(3) NULL` |
| LeaseUntilUtc | `datetime2(3)` | Yes | `LeaseUntilUtc datetime2(3) NULL` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id uniqueidentifier NOT NULL CONSTRAINT PK_UnitBatches PRIMARY KEY,
    RequestId uniqueidentifier NOT NULL CONSTRAINT UQ_UnitBatches_Request UNIQUE,
    RequestedByUserId int NOT NULL,
    ProjectId int NOT NULL,
    BuildingId int NULL,
    UnitType varchar(16) NOT NULL CONSTRAINT CK_UnitBatches_Type CHECK (UnitType IN ('Apartment','Townhouse','Villa')),
    PayloadJson nvarchar(max) NOT NULL CONSTRAINT CK_UnitBatches_Json CHECK (ISJSON(PayloadJson)=1),
    RequestedCount int NOT NULL CONSTRAINT CK_UnitBatches_Count CHECK (RequestedCount BETWEEN 1 AND 500),
    CreatedCount int NOT NULL CONSTRAINT DF_UnitBatches_CreatedCount DEFAULT 0,
    Status varchar(16) NOT NULL CONSTRAINT DF_UnitBatches_Status DEFAULT 'Queued',
    Attempts int NOT NULL CONSTRAINT DF_UnitBatches_Attempts DEFAULT 0,
    ErrorMessage nvarchar(2000) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UnitBatches_Created DEFAULT SYSUTCDATETIME(),
    StartedAtUtc datetime2(3) NULL,
    CompletedAtUtc datetime2(3) NULL,
    LeaseUntilUtc datetime2(3) NULL,
    Version rowversion NOT NULL,
    CONSTRAINT CK_UnitBatches_Status CHECK (Status IN ('Queued','Processing','Completed','Failed')),
    CONSTRAINT CK_UnitBatches_CreatedCount CHECK (CreatedCount BETWEEN 0 AND RequestedCount),
    CONSTRAINT CK_UnitBatches_Atomic CHECK ((Status='Completed' AND CreatedCount=RequestedCount) OR (Status<>'Completed' AND CreatedCount=0)),
    CONSTRAINT CK_UnitBatches_Attempts CHECK (Attempts>=0),
    CONSTRAINT CK_UnitBatches_Building CHECK ((UnitType='Apartment' AND BuildingId IS NOT NULL) OR (UnitType IN ('Townhouse','Villa') AND BuildingId IS NULL)),
    CONSTRAINT FK_UnitBatches_User FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_UnitBatches_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
    CONSTRAINT FK_UnitBatches_Building FOREIGN KEY (ProjectId,BuildingId) REFERENCES dbo.Buildings(ProjectId,Id)
```

## Units

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `int` | No | `Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Units PRIMARY KEY` |
| ProjectId | `int` | No | `ProjectId int NOT NULL` |
| BuildingId | `int` | Yes | `BuildingId int NULL` |
| OriginBatchId | `uniqueidentifier` | No | `OriginBatchId uniqueidentifier NOT NULL` |
| Code | `nvarchar(50)` | No | `Code nvarchar(50) NOT NULL` |
| UnitType | `varchar(16)` | No | `UnitType varchar(16) NOT NULL` |
| FloorNumber | `smallint` | Yes | `FloorNumber smallint NULL` |
| OwnFloorCount | `smallint` | Yes | `OwnFloorCount smallint NULL` |
| UsableArea | `decimal(12,2)` | Yes | `UsableArea decimal(12,2) NULL` |
| LandArea | `decimal(12,2)` | Yes | `LandArea decimal(12,2) NULL` |
| FootprintArea | `decimal(12,2)` | Yes | `FootprintArea decimal(12,2) NULL` |
| GardenArea | `decimal(12,2)` | Yes | `GardenArea decimal(12,2) NULL` |
| BedroomCount | `smallint` | Yes | `BedroomCount smallint NULL` |
| BathroomCount | `smallint` | Yes | `BathroomCount smallint NULL` |
| PriceVnd | `decimal(18,0)` | Yes | `PriceVnd decimal(18,0) NULL` |
| ManagementStatus | `varchar(16)` | No | `ManagementStatus varchar(16) NOT NULL CONSTRAINT DF_Units_Status DEFAULT 'Managed'` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Units_Created DEFAULT SYSUTCDATETIME()` |
| UpdatedAtUtc | `datetime2(3)` | No | `UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Units_Updated DEFAULT SYSUTCDATETIME()` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Units PRIMARY KEY,
    ProjectId int NOT NULL,
    BuildingId int NULL,
    OriginBatchId uniqueidentifier NOT NULL,
    Code nvarchar(50) NOT NULL,
    UnitType varchar(16) NOT NULL,
    FloorNumber smallint NULL,
    OwnFloorCount smallint NULL,
    UsableArea decimal(12,2) NULL,
    LandArea decimal(12,2) NULL,
    FootprintArea decimal(12,2) NULL,
    GardenArea decimal(12,2) NULL,
    BedroomCount smallint NULL,
    BathroomCount smallint NULL,
    PriceVnd decimal(18,0) NULL,
    ManagementStatus varchar(16) NOT NULL CONSTRAINT DF_Units_Status DEFAULT 'Managed',
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Units_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Units_Updated DEFAULT SYSUTCDATETIME(),
    Version rowversion NOT NULL,
    CONSTRAINT UQ_Units_ProjectCode UNIQUE (ProjectId,Code),
    CONSTRAINT CK_Units_Type CHECK (UnitType IN ('Apartment','Townhouse','Villa')),
    CONSTRAINT CK_Units_Status CHECK (ManagementStatus IN ('Managed','Unmanaged','Sold')),
    CONSTRAINT CK_Units_TypeFields CHECK (
        (UnitType='Apartment' AND BuildingId IS NOT NULL AND FloorNumber IS NOT NULL AND OwnFloorCount IS NULL AND LandArea IS NULL AND FootprintArea IS NULL AND GardenArea IS NULL)
        OR (UnitType='Townhouse' AND BuildingId IS NULL AND FloorNumber IS NULL AND UsableArea IS NULL AND GardenArea IS NULL)
        OR (UnitType='Villa' AND BuildingId IS NULL AND FloorNumber IS NULL AND UsableArea IS NULL)),
    CONSTRAINT CK_Units_Floor CHECK (FloorNumber IS NULL OR FloorNumber BETWEEN 1 AND 99),
    CONSTRAINT CK_Units_OwnFloor CHECK (OwnFloorCount IS NULL OR OwnFloorCount>0),
    CONSTRAINT CK_Units_UsableArea CHECK (UsableArea IS NULL OR UsableArea>0),
    CONSTRAINT CK_Units_LandArea CHECK (LandArea IS NULL OR LandArea>0),
    CONSTRAINT CK_Units_FootprintArea CHECK (FootprintArea IS NULL OR FootprintArea>0),
    CONSTRAINT CK_Units_GardenArea CHECK (GardenArea IS NULL OR GardenArea>=0),
    CONSTRAINT CK_Units_Bedrooms CHECK (BedroomCount IS NULL OR BedroomCount>=0),
    CONSTRAINT CK_Units_Bathrooms CHECK (BathroomCount IS NULL OR BathroomCount>=0),
    CONSTRAINT CK_Units_Price CHECK (PriceVnd IS NULL OR PriceVnd>0),
    CONSTRAINT FK_Units_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
    CONSTRAINT FK_Units_Building FOREIGN KEY (ProjectId,BuildingId) REFERENCES dbo.Buildings(ProjectId,Id),
    CONSTRAINT FK_Units_Batch FOREIGN KEY (OriginBatchId) REFERENCES dbo.UnitBatches(Id)
```

## UnitAttributes

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `int` | No | `Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnitAttributes PRIMARY KEY` |
| UnitId | `int` | No | `UnitId int NOT NULL` |
| Name | `nvarchar(100)` | No | `Name nvarchar(100) NOT NULL` |
| Value | `nvarchar(1000)` | No | `Value nvarchar(1000) NOT NULL` |

Table-level constraints:

```sql
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnitAttributes PRIMARY KEY,
    UnitId int NOT NULL,
    Name nvarchar(100) NOT NULL,
    Value nvarchar(1000) NOT NULL,
    CONSTRAINT UQ_UnitAttributes_Name UNIQUE (UnitId,Name),
    CONSTRAINT CK_UnitAttributes_Name CHECK (LEN(LTRIM(RTRIM(Name)))>0),
    CONSTRAINT FK_UnitAttributes_Unit FOREIGN KEY (UnitId) REFERENCES dbo.Units(Id) ON DELETE CASCADE
```

## UnitStatusHistory

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `bigint` | No | `Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnitStatusHistory PRIMARY KEY` |
| UnitId | `int` | No | `UnitId int NOT NULL` |
| ChangedByUserId | `int` | No | `ChangedByUserId int NOT NULL` |
| OldStatus | `varchar(16)` | No | `OldStatus varchar(16) NOT NULL` |
| NewStatus | `varchar(16)` | No | `NewStatus varchar(16) NOT NULL` |
| Reason | `nvarchar(500)` | No | `Reason nvarchar(500) NOT NULL` |
| ChangedAtUtc | `datetime2(3)` | No | `ChangedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UnitStatusHistory_Changed DEFAULT SYSUTCDATETIME()` |

Table-level constraints:

```sql
    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnitStatusHistory PRIMARY KEY,
    UnitId int NOT NULL,
    ChangedByUserId int NOT NULL,
    OldStatus varchar(16) NOT NULL,
    NewStatus varchar(16) NOT NULL,
    Reason nvarchar(500) NOT NULL,
    ChangedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UnitStatusHistory_Changed DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_UnitStatusHistory_Status CHECK (OldStatus IN ('Managed','Unmanaged','Sold') AND NewStatus IN ('Managed','Unmanaged','Sold') AND OldStatus<>NewStatus),
    CONSTRAINT CK_UnitStatusHistory_Reason CHECK (LEN(LTRIM(RTRIM(Reason)))>0),
    CONSTRAINT FK_UnitStatusHistory_Unit FOREIGN KEY (UnitId) REFERENCES dbo.Units(Id),
    CONSTRAINT FK_UnitStatusHistory_User FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users(Id)
```

## SummaryRuns

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `uniqueidentifier` | No | `Id uniqueidentifier NOT NULL CONSTRAINT PK_SummaryRuns PRIMARY KEY` |
| EventId | `uniqueidentifier` | No | `EventId uniqueidentifier NOT NULL CONSTRAINT UQ_SummaryRuns_Event UNIQUE` |
| ScheduledDate | `date` | Yes | `ScheduledDate date NULL` |
| RequestedByUserId | `int` | Yes | `RequestedByUserId int NULL` |
| Status | `varchar(16)` | No | `Status varchar(16) NOT NULL CONSTRAINT DF_SummaryRuns_Status DEFAULT 'Queued'` |
| AsOfUtc | `datetime2(3)` | Yes | `AsOfUtc datetime2(3) NULL` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_SummaryRuns_Created DEFAULT SYSUTCDATETIME()` |
| CompletedAtUtc | `datetime2(3)` | Yes | `CompletedAtUtc datetime2(3) NULL` |
| Attempts | `int` | No | `Attempts int NOT NULL CONSTRAINT DF_SummaryRuns_Attempts DEFAULT 0` |
| LeaseUntilUtc | `datetime2(3)` | Yes | `LeaseUntilUtc datetime2(3) NULL` |
| ErrorMessage | `nvarchar(2000)` | Yes | `ErrorMessage nvarchar(2000) NULL` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id uniqueidentifier NOT NULL CONSTRAINT PK_SummaryRuns PRIMARY KEY,
    EventId uniqueidentifier NOT NULL CONSTRAINT UQ_SummaryRuns_Event UNIQUE,
    ScheduledDate date NULL,
    RequestedByUserId int NULL,
    Status varchar(16) NOT NULL CONSTRAINT DF_SummaryRuns_Status DEFAULT 'Queued',
    AsOfUtc datetime2(3) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_SummaryRuns_Created DEFAULT SYSUTCDATETIME(),
    CompletedAtUtc datetime2(3) NULL,
    Attempts int NOT NULL CONSTRAINT DF_SummaryRuns_Attempts DEFAULT 0,
    LeaseUntilUtc datetime2(3) NULL,
    ErrorMessage nvarchar(2000) NULL,
    Version rowversion NOT NULL,
    CONSTRAINT CK_SummaryRuns_Status CHECK (Status IN ('Queued','Processing','Completed','Failed')),
    CONSTRAINT CK_SummaryRuns_Attempts CHECK (Attempts>=0),
    CONSTRAINT FK_SummaryRuns_User FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id)
```

## ProjectSummaries

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `bigint` | No | `Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProjectSummaries PRIMARY KEY` |
| SummaryRunId | `uniqueidentifier` | No | `SummaryRunId uniqueidentifier NOT NULL` |
| ProjectId | `int` | No | `ProjectId int NOT NULL` |
| TotalCount | `int` | No | `TotalCount int NOT NULL` |
| ManagedCount | `int` | No | `ManagedCount int NOT NULL` |
| UnmanagedCount | `int` | No | `UnmanagedCount int NOT NULL` |
| SoldCount | `int` | No | `SoldCount int NOT NULL` |
| ManagedWithoutPriceCount | `int` | No | `ManagedWithoutPriceCount int NOT NULL` |

Table-level constraints:

```sql
    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProjectSummaries PRIMARY KEY,
    SummaryRunId uniqueidentifier NOT NULL,
    ProjectId int NOT NULL,
    TotalCount int NOT NULL,
    ManagedCount int NOT NULL,
    UnmanagedCount int NOT NULL,
    SoldCount int NOT NULL,
    ManagedWithoutPriceCount int NOT NULL,
    CONSTRAINT UQ_ProjectSummaries_RunProject UNIQUE (SummaryRunId,ProjectId),
    CONSTRAINT CK_ProjectSummaries_Counts CHECK (TotalCount>=0 AND ManagedCount>=0 AND UnmanagedCount>=0 AND SoldCount>=0 AND TotalCount=ManagedCount+UnmanagedCount+SoldCount AND ManagedWithoutPriceCount BETWEEN 0 AND ManagedCount),
    CONSTRAINT FK_ProjectSummaries_Run FOREIGN KEY (SummaryRunId) REFERENCES dbo.SummaryRuns(Id),
    CONSTRAINT FK_ProjectSummaries_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id)
```

## OutboxMessages

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `uniqueidentifier` | No | `Id uniqueidentifier NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY` |
| Kind | `varchar(32)` | No | `Kind varchar(32) NOT NULL` |
| OperationId | `uniqueidentifier` | No | `OperationId uniqueidentifier NOT NULL` |
| PayloadJson | `nvarchar(max)` | No | `PayloadJson nvarchar(max) NOT NULL` |
| OccurredAtUtc | `datetime2(3)` | No | `OccurredAtUtc datetime2(3) NOT NULL CONSTRAINT DF_OutboxMessages_Occurred DEFAULT SYSUTCDATETIME()` |
| PublishedAtUtc | `datetime2(3)` | Yes | `PublishedAtUtc datetime2(3) NULL` |
| PublishAttempts | `int` | No | `PublishAttempts int NOT NULL CONSTRAINT DF_OutboxMessages_Attempts DEFAULT 0` |
| ErrorMessage | `nvarchar(2000)` | Yes | `ErrorMessage nvarchar(2000) NULL` |

Table-level constraints:

```sql
    Id uniqueidentifier NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
    Kind varchar(32) NOT NULL,
    OperationId uniqueidentifier NOT NULL,
    PayloadJson nvarchar(max) NOT NULL,
    OccurredAtUtc datetime2(3) NOT NULL CONSTRAINT DF_OutboxMessages_Occurred DEFAULT SYSUTCDATETIME(),
    PublishedAtUtc datetime2(3) NULL,
    PublishAttempts int NOT NULL CONSTRAINT DF_OutboxMessages_Attempts DEFAULT 0,
    ErrorMessage nvarchar(2000) NULL,
    CONSTRAINT CK_OutboxMessages_Kind CHECK (Kind IN ('BatchRequested','SummaryRequested','BackupRequested')),
    CONSTRAINT CK_OutboxMessages_Json CHECK (ISJSON(PayloadJson)=1),
    CONSTRAINT CK_OutboxMessages_Attempts CHECK (PublishAttempts>=0)
```

## BackupRuns

| Column | SQL type | Nullable | Definition / inline constraint |
|---|---|---|---|
| Id | `uniqueidentifier` | No | `Id uniqueidentifier NOT NULL CONSTRAINT PK_BackupRuns PRIMARY KEY` |
| EventId | `uniqueidentifier` | No | `EventId uniqueidentifier NOT NULL CONSTRAINT UQ_BackupRuns_Event UNIQUE` |
| TriggerKind | `varchar(16)` | No | `TriggerKind varchar(16) NOT NULL` |
| ScheduledDate | `date` | Yes | `ScheduledDate date NULL` |
| RequestedByUserId | `int` | Yes | `RequestedByUserId int NULL` |
| Status | `varchar(16)` | No | `Status varchar(16) NOT NULL CONSTRAINT DF_BackupRuns_Status DEFAULT 'Queued'` |
| FileName | `nvarchar(200)` | Yes | `FileName nvarchar(200) NULL` |
| Retained | `bit` | No | `Retained bit NOT NULL CONSTRAINT DF_BackupRuns_Retained DEFAULT 0` |
| VerifiedAtUtc | `datetime2(3)` | Yes | `VerifiedAtUtc datetime2(3) NULL` |
| CreatedAtUtc | `datetime2(3)` | No | `CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_BackupRuns_Created DEFAULT SYSUTCDATETIME()` |
| CompletedAtUtc | `datetime2(3)` | Yes | `CompletedAtUtc datetime2(3) NULL` |
| Attempts | `int` | No | `Attempts int NOT NULL CONSTRAINT DF_BackupRuns_Attempts DEFAULT 0` |
| LeaseUntilUtc | `datetime2(3)` | Yes | `LeaseUntilUtc datetime2(3) NULL` |
| ErrorMessage | `nvarchar(2000)` | Yes | `ErrorMessage nvarchar(2000) NULL` |
| Version | `rowversion` | No | `Version rowversion NOT NULL` |

Table-level constraints:

```sql
    Id uniqueidentifier NOT NULL CONSTRAINT PK_BackupRuns PRIMARY KEY,
    EventId uniqueidentifier NOT NULL CONSTRAINT UQ_BackupRuns_Event UNIQUE,
    TriggerKind varchar(16) NOT NULL,
    ScheduledDate date NULL,
    RequestedByUserId int NULL,
    Status varchar(16) NOT NULL CONSTRAINT DF_BackupRuns_Status DEFAULT 'Queued',
    FileName nvarchar(200) NULL,
    Retained bit NOT NULL CONSTRAINT DF_BackupRuns_Retained DEFAULT 0,
    VerifiedAtUtc datetime2(3) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_BackupRuns_Created DEFAULT SYSUTCDATETIME(),
    CompletedAtUtc datetime2(3) NULL,
    Attempts int NOT NULL CONSTRAINT DF_BackupRuns_Attempts DEFAULT 0,
    LeaseUntilUtc datetime2(3) NULL,
    ErrorMessage nvarchar(2000) NULL,
    Version rowversion NOT NULL,
    CONSTRAINT CK_BackupRuns_Trigger CHECK (TriggerKind IN ('Daily','Change','Manual')),
    CONSTRAINT CK_BackupRuns_Status CHECK (Status IN ('Queued','Processing','Completed','Failed')),
    CONSTRAINT CK_BackupRuns_Daily CHECK ((TriggerKind='Daily' AND ScheduledDate IS NOT NULL) OR (TriggerKind IN ('Change','Manual') AND ScheduledDate IS NULL)),
    CONSTRAINT CK_BackupRuns_Retained CHECK (Retained=0 OR (Status='Completed' AND FileName IS NOT NULL AND VerifiedAtUtc IS NOT NULL)),
    CONSTRAINT CK_BackupRuns_Attempts CHECK (Attempts>=0),
    CONSTRAINT FK_BackupRuns_User FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id)
```

## Indexes

```sql
CREATE INDEX IX_Projects_Developer ON dbo.Projects(DeveloperId);
CREATE INDEX IX_Units_Catalogue ON dbo.Units(ProjectId,ManagementStatus,UnitType,Id) INCLUDE (Code,PriceVnd,BuildingId);
CREATE INDEX IX_Units_Price ON dbo.Units(PriceVnd,Id) WHERE PriceVnd IS NOT NULL;
CREATE INDEX IX_Units_Building ON dbo.Units(BuildingId,FloorNumber);
CREATE INDEX IX_Units_Batch ON dbo.Units(OriginBatchId);
CREATE INDEX IX_UnitStatusHistory_UnitTime ON dbo.UnitStatusHistory(UnitId,ChangedAtUtc DESC,Id DESC);
CREATE INDEX IX_UnitBatches_Queue ON dbo.UnitBatches(Status,CreatedAtUtc);
CREATE UNIQUE INDEX UX_SummaryRuns_Daily ON dbo.SummaryRuns(ScheduledDate) WHERE ScheduledDate IS NOT NULL;
CREATE INDEX IX_SummaryRuns_Completed ON dbo.SummaryRuns(Status,CompletedAtUtc);
CREATE INDEX IX_ProjectSummaries_Project ON dbo.ProjectSummaries(ProjectId);
CREATE INDEX IX_OutboxMessages_Unpublished ON dbo.OutboxMessages(OccurredAtUtc) INCLUDE (Kind,OperationId) WHERE PublishedAtUtc IS NULL;
CREATE UNIQUE INDEX UX_BackupRuns_Daily ON dbo.BackupRuns(ScheduledDate) WHERE ScheduledDate IS NOT NULL;
CREATE INDEX IX_BackupRuns_Queue ON dbo.BackupRuns(Status,CreatedAtUtc);


```

## Service-level invariants

- Service invariants not expressible as row-local CHECK constraints:
- Apartment FloorNumber <= Building.FloorCount; don't lower floor count below existing units.
- Unit's Project/Building/Type and OriginBatch must match the accepted Batch payload.
- Status update + immutable history + backup outbox event commit together.
- Eligible Unit deletion (no history) and backup event commit together.
- Every actual business mutation enqueues one backup event; operational bookkeeping does not.
- Scheduled requests create Run + OutboxMessage atomically before publishing.
- Summary/Backup EventId equals its corresponding outbox Id; enforced by handlers.
- Immutable identity, trimmed codes/names and fractional-price rejection enforced by Services.
