SET NOCOUNT ON;

/*
    Read-only preview for finance rows that may have been introduced by the
    historical backfill in 20260715_CreateWebApprovalAndReceiptSupport.sql.

    This script does not update or delete application data. The temporary table
    exists only for this session and is used to produce consistent detail and
    summary result sets for manual review.
*/

IF OBJECT_ID('dbo.tbReportFinanceTracking', 'U') IS NULL
   OR OBJECT_ID('dbo.tbReportDetails', 'U') IS NULL
   OR OBJECT_ID('dbo.tbUserRegistration', 'U') IS NULL
BEGIN
    RAISERROR('Required ER System finance preview tables are missing.', 16, 1);
    RETURN;
END;

SELECT
    finance.ReportID,
    report.ERFReferenceNo,
    report.UserID,
    users.Fullname AS EmployeeName,
    report.ReportDateFrom,
    report.ReportDateTo,
    report.ReportFileStatus,
    report.ReportPrintStatus,
    finance.FinanceStatus,
    finance.PhysicalReceiptsReceived,
    finance.PhysicalReceiptsReceivedBy,
    finance.PhysicalReceiptsReceivedDate,
    finance.FinanceRemarks,
    finance.ScannedReceiptsDeletedDate,
    CASE
        WHEN LTRIM(RTRIM(ISNULL(finance.FinanceStatus, ''))) = 'Pending'
         AND finance.PhysicalReceiptsReceived = 0
         AND finance.PhysicalReceiptsReceivedBy IS NULL
         AND finance.PhysicalReceiptsReceivedDate IS NULL
         AND NULLIF(LTRIM(RTRIM(ISNULL(finance.FinanceRemarks, ''))), '') IS NULL
         AND finance.ScannedReceiptsDeletedDate IS NULL
        THEN 'Cleanup candidate'
        ELSE 'Keep - finance activity exists'
    END AS ReviewClassification
INTO #FinanceCleanupPreview
FROM dbo.tbReportFinanceTracking finance
INNER JOIN dbo.tbReportDetails report ON report.ID = finance.ReportID
LEFT JOIN dbo.tbUserRegistration users ON users.UserID = report.UserID;

SELECT
    ReportID,
    ERFReferenceNo,
    UserID,
    EmployeeName,
    ReportDateFrom,
    ReportDateTo,
    ReportFileStatus,
    ReportPrintStatus,
    FinanceStatus,
    PhysicalReceiptsReceived,
    PhysicalReceiptsReceivedBy,
    PhysicalReceiptsReceivedDate,
    FinanceRemarks,
    ScannedReceiptsDeletedDate,
    ReviewClassification
FROM #FinanceCleanupPreview
ORDER BY ReportDateFrom DESC, EmployeeName, ReportID;

SELECT
    CASE WHEN ReportDateFrom IS NULL THEN NULL ELSE DATEPART(year, ReportDateFrom) END AS ReportYear,
    ReviewClassification,
    COUNT(*) AS ReportCount
FROM #FinanceCleanupPreview
GROUP BY
    CASE WHEN ReportDateFrom IS NULL THEN NULL ELSE DATEPART(year, ReportDateFrom) END,
    ReviewClassification
ORDER BY ReportYear DESC, ReviewClassification;

SELECT
    ReviewClassification,
    COUNT(*) AS ReportCount
FROM #FinanceCleanupPreview
GROUP BY ReviewClassification
ORDER BY ReviewClassification;

DROP TABLE #FinanceCleanupPreview;
