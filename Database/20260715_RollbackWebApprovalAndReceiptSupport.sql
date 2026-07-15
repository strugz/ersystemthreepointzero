SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportFinanceTracking') AND name = 'UX_tbReportFinanceTracking_ReportID')
    DROP INDEX UX_tbReportFinanceTracking_ReportID ON dbo.tbReportFinanceTracking;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbUserAuthority') AND name = 'IX_tbUserAuthority_AuthorityID_UserID_Sort')
    DROP INDEX IX_tbUserAuthority_AuthorityID_UserID_Sort ON dbo.tbUserAuthority;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportAuthority') AND name = 'IX_tbReportAuthority_ReportID_SignID')
    DROP INDEX IX_tbReportAuthority_ReportID_SignID ON dbo.tbReportAuthority;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportDetails') AND name = 'IX_tbReportDetails_WebQueue')
    DROP INDEX IX_tbReportDetails_WebQueue ON dbo.tbReportDetails;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbExpenseDetails') AND name = 'IX_tbExpenseDetails_ReportID_Sort')
    DROP INDEX IX_tbExpenseDetails_ReportID_Sort ON dbo.tbExpenseDetails;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbCashAdvance') AND name = 'IX_tbCashAdvance_ReportID')
    DROP INDEX IX_tbCashAdvance_ReportID ON dbo.tbCashAdvance;

IF OBJECT_ID('dbo.tbWebWorkflowAudit', 'U') IS NOT NULL DROP TABLE dbo.tbWebWorkflowAudit;
IF OBJECT_ID('dbo.tbWebLoginSecurity', 'U') IS NOT NULL DROP TABLE dbo.tbWebLoginSecurity;
IF COL_LENGTH('dbo.tbReportFinanceTracking', 'RowVersion') IS NOT NULL ALTER TABLE dbo.tbReportFinanceTracking DROP COLUMN RowVersion;
IF COL_LENGTH('dbo.tbReportDetails', 'RowVersion') IS NOT NULL ALTER TABLE dbo.tbReportDetails DROP COLUMN RowVersion;

COMMIT TRANSACTION;
GO
