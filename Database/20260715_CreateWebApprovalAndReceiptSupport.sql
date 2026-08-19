SET XACT_ABORT ON;

DECLARE @ProductVersion nvarchar(128);
DECLARE @EngineMajorVersion int;
SET @ProductVersion = CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion'));
SET @EngineMajorVersion = CONVERT(int, LEFT(@ProductVersion, CHARINDEX('.', @ProductVersion + '.') - 1));
IF @EngineMajorVersion IS NULL OR @EngineMajorVersion < 10
BEGIN
    RAISERROR('ER System Web requires SQL Server 2008 (10.x) or later.', 16, 1);
    RETURN;
END;

DECLARE @CompatibilityLevel int;
SET @CompatibilityLevel = (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME());
IF @CompatibilityLevel <> 100
BEGIN
    RAISERROR('ER System Web v1 requires database compatibility level 100.', 16, 1);
    RETURN;
END;

/* Fail before changing the schema when live data violates required invariants. */
IF EXISTS (SELECT ReportID FROM dbo.tbReportFinanceTracking GROUP BY ReportID HAVING ReportID IS NULL OR COUNT(*) > 1)
BEGIN
    RAISERROR('Finance tracking contains null or duplicate ReportID values.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1 FROM dbo.tbReportFinanceTracking finance
    LEFT JOIN dbo.tbReportDetails report ON report.ID = finance.ReportID
    WHERE report.ID IS NULL)
BEGIN
    RAISERROR('Finance tracking contains orphaned ReportID values.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT UserID FROM dbo.tbUserRegistration WHERE UserID IS NOT NULL GROUP BY UserID HAVING COUNT(*) > 1)
BEGIN
    RAISERROR('User registration contains duplicate UserID values.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT UPPER(LTRIM(RTRIM(username)))
    FROM dbo.tbUserRegistration
    WHERE NULLIF(LTRIM(RTRIM(username)), '') IS NOT NULL
    GROUP BY UPPER(LTRIM(RTRIM(username)))
    HAVING COUNT(*) > 1)
BEGIN
    RAISERROR('User registration contains duplicate normalized usernames.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT UserID, AuthorityID, Sort
    FROM dbo.tbUserAuthority
    GROUP BY UserID, AuthorityID, Sort
    HAVING COUNT(*) > 1)
BEGIN
    RAISERROR('Approval authority contains duplicate employee, approver, and sequence rows.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1 FROM dbo.tbReportAuthority authority
    LEFT JOIN dbo.tbReportDetails report ON report.ID = authority.ReportID
    LEFT JOIN dbo.tbUserRegistration signer ON signer.UserID = authority.SignID
    LEFT JOIN dbo.tbUserRegistration ownerUser ON ownerUser.UserID = authority.UserID
    WHERE report.ID IS NULL OR signer.UserID IS NULL OR ownerUser.UserID IS NULL)
BEGIN
    RAISERROR('Report authority contains orphaned report or user references.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.tbReportDetails', 'RowVersion') IS NULL
    ALTER TABLE dbo.tbReportDetails ADD RowVersion rowversion NOT NULL;

IF COL_LENGTH('dbo.tbReportFinanceTracking', 'RowVersion') IS NULL
    ALTER TABLE dbo.tbReportFinanceTracking ADD RowVersion rowversion NOT NULL;

IF OBJECT_ID('dbo.tbWebLoginSecurity', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbWebLoginSecurity
    (
        UserID int NOT NULL CONSTRAINT PK_tbWebLoginSecurity PRIMARY KEY,
        FailedAttemptCount int NOT NULL CONSTRAINT DF_tbWebLoginSecurity_FailedAttemptCount DEFAULT (0),
        FirstFailedAttemptUtc datetime2(0) NULL,
        LockoutEndUtc datetime2(0) NULL,
        LastSuccessfulLoginUtc datetime2(0) NULL
    );
END;

IF OBJECT_ID('dbo.tbWebWorkflowAudit', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbWebWorkflowAudit
    (
        ID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbWebWorkflowAudit PRIMARY KEY,
        ReportID varchar(50) NOT NULL,
        ActorUserID int NOT NULL,
        EventType varchar(40) NOT NULL,
        PreviousState varchar(100) NULL,
        NewState varchar(100) NULL,
        Remarks nvarchar(1000) NULL,
        OccurredAtUtc datetime2(0) NOT NULL,
        CorrelationID uniqueidentifier NOT NULL
    );
END;

/* Existing approved reports must be visible to Finance without mutating a GET request. */
INSERT INTO dbo.tbReportFinanceTracking
    (ReportID, FinanceStatus, PhysicalReceiptsReceived)
SELECT report.ID, 'Pending', 0
FROM dbo.tbReportDetails report
WHERE report.ReportFileStatus = '0'
  AND report.ReportPrintStatus = '0'
  AND NOT EXISTS (SELECT 1 FROM dbo.tbReportFinanceTracking finance WHERE finance.ReportID = report.ID);

UPDATE dbo.tbReportFinanceTracking
SET FinanceStatus = 'Pending'
WHERE FinanceStatus IS NULL OR LTRIM(RTRIM(FinanceStatus)) = '';

ALTER TABLE dbo.tbReportFinanceTracking ALTER COLUMN ReportID varchar(50) NOT NULL;
ALTER TABLE dbo.tbReportFinanceTracking ALTER COLUMN FinanceStatus varchar(100) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportFinanceTracking') AND name = 'UX_tbReportFinanceTracking_ReportID')
    CREATE UNIQUE INDEX UX_tbReportFinanceTracking_ReportID ON dbo.tbReportFinanceTracking(ReportID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbUserAuthority') AND name = 'IX_tbUserAuthority_AuthorityID_UserID_Sort')
    CREATE INDEX IX_tbUserAuthority_AuthorityID_UserID_Sort ON dbo.tbUserAuthority(AuthorityID, UserID, Sort);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportAuthority') AND name = 'IX_tbReportAuthority_ReportID_SignID')
    CREATE INDEX IX_tbReportAuthority_ReportID_SignID ON dbo.tbReportAuthority(ReportID, SignID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportDetails') AND name = 'IX_tbReportDetails_WebQueue')
    CREATE INDEX IX_tbReportDetails_WebQueue ON dbo.tbReportDetails(ReportFileStatus, ReportPrintStatus, UserID, ReportDateFrom);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbExpenseDetails') AND name = 'IX_tbExpenseDetails_ReportID_Sort')
    CREATE INDEX IX_tbExpenseDetails_ReportID_Sort ON dbo.tbExpenseDetails(ReportID, Sort);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbCashAdvance') AND name = 'IX_tbCashAdvance_ReportID')
    CREATE INDEX IX_tbCashAdvance_ReportID ON dbo.tbCashAdvance(ReportID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbWebWorkflowAudit') AND name = 'IX_tbWebWorkflowAudit_ReportID_OccurredAtUtc')
    CREATE INDEX IX_tbWebWorkflowAudit_ReportID_OccurredAtUtc ON dbo.tbWebWorkflowAudit(ReportID, OccurredAtUtc DESC);

COMMIT TRANSACTION;
GO
