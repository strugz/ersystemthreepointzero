SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
    Run 20260715_PreviewHistoricalFinanceTrackingCleanup.sql first.

    Add only manually reviewed internal ReportID values below. This script
    deletes exclusively from dbo.tbReportFinanceTracking and rolls back by
    default. Set @CommitChanges to 1 only after reviewing both preview result
    sets produced inside the transaction.
*/

DECLARE @CommitChanges bit;
SET @CommitChanges = 0;

DECLARE @ApprovedReportIDs TABLE
(
    ReportID varchar(50) NOT NULL PRIMARY KEY
);

/* Example only; keep commented and replace with reviewed values.
INSERT INTO @ApprovedReportIDs (ReportID) VALUES ('00000000-0000-0000-0000-000000000000');
*/

IF NOT EXISTS (SELECT 1 FROM @ApprovedReportIDs)
BEGIN
    RAISERROR('No reviewed ReportID values were supplied. Nothing was changed.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('dbo.tbReportFinanceTracking', 'U') IS NULL
BEGIN
    RAISERROR('dbo.tbReportFinanceTracking does not exist.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

IF EXISTS
(
    SELECT 1
    FROM @ApprovedReportIDs approved
    LEFT JOIN dbo.tbReportFinanceTracking finance ON finance.ReportID = approved.ReportID
    WHERE finance.ReportID IS NULL
)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('At least one reviewed ReportID has no finance tracking row. Nothing was changed.', 16, 1);
    RETURN;
END;

IF EXISTS
(
    SELECT 1
    FROM @ApprovedReportIDs approved
    INNER JOIN dbo.tbReportFinanceTracking finance ON finance.ReportID = approved.ReportID
    WHERE LTRIM(RTRIM(ISNULL(finance.FinanceStatus, ''))) <> 'Pending'
       OR finance.PhysicalReceiptsReceived <> 0
       OR finance.PhysicalReceiptsReceivedBy IS NOT NULL
       OR finance.PhysicalReceiptsReceivedDate IS NOT NULL
       OR NULLIF(LTRIM(RTRIM(ISNULL(finance.FinanceRemarks, ''))), '') IS NOT NULL
       OR finance.ScannedReceiptsDeletedDate IS NOT NULL
)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Finance activity exists for at least one reviewed ReportID. Nothing was changed.', 16, 1);
    RETURN;
END;

SELECT
    finance.ReportID,
    finance.FinanceStatus,
    finance.PhysicalReceiptsReceived,
    finance.PhysicalReceiptsReceivedBy,
    finance.PhysicalReceiptsReceivedDate,
    finance.FinanceRemarks,
    finance.ScannedReceiptsDeletedDate
FROM dbo.tbReportFinanceTracking finance
INNER JOIN @ApprovedReportIDs approved ON approved.ReportID = finance.ReportID
ORDER BY finance.ReportID;

DECLARE @ExpectedDeleteCount int;
DECLARE @DeletedCount int;
SELECT @ExpectedDeleteCount = COUNT(*) FROM @ApprovedReportIDs;

DELETE finance
FROM dbo.tbReportFinanceTracking finance
INNER JOIN @ApprovedReportIDs approved ON approved.ReportID = finance.ReportID;

SET @DeletedCount = @@ROWCOUNT;

IF @DeletedCount <> @ExpectedDeleteCount
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('The deleted row count did not match the reviewed row count. Nothing was changed.', 16, 1);
    RETURN;
END;

SELECT
    @ExpectedDeleteCount AS ReviewedReportCount,
    @DeletedCount AS DeletedReportCount,
    CASE WHEN @CommitChanges = 1 THEN 'Commit requested' ELSE 'Dry run - rollback requested' END AS ExecutionMode;

IF @CommitChanges = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Reviewed finance tracking rows were deleted and the transaction was committed.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Dry run completed. The transaction was rolled back; no rows were changed.';
END;
