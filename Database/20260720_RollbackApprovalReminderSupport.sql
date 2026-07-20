SET XACT_ABORT ON;
GO

/*
    This rollback disables the reminder integration and removes its delivery ledger.
    NotificationEmail is intentionally retained so rollback cannot destroy contact data.
*/

IF OBJECT_ID('dbo.sp_Notify', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp_Notify AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp_Notify
    @ReportID varchar(100),
    @Status varchar(20) = 'APPROVE'
AS
BEGIN
    DECLARE @StrValue varchar(2000);
    DECLARE @StrLocation varchar(2000);

    SELECT @StrLocation = 'D:\ERSHARE\' + RIGHT(NEWID(), 12) + '.txt';

    SELECT @StrValue =
        (SELECT department.emp_Dept
         FROM dbo.tblDept department
         WHERE department.ID = account.DeptID) + '|' + @Status + '|' +
        CONVERT(varchar(17), GETDATE()) + '|' +
        ISNULL(report.ReportDateFiled, '') + '||' +
        ISNULL(CONVERT(varchar(1), report.ReportNumberStatus), '') + '|' +
        ISNULL(report.ReportPrintStatus, '') + '|' +
        ISNULL(report.ReportFileStatus, '') + '|' +
        ISNULL(report.ReportDescription, '') + '|' +
        ISNULL(report.ReportCancelNote, '') + '|' +
        ISNULL(account.username, '') + '|' +
        ISNULL(account.approver1, '') + '|' +
        ISNULL(account.approver2, '')
    FROM dbo.tbReportDetails report
    LEFT JOIN dbo.tbUserRegistration account ON report.UserID = account.UserID
    WHERE report.ID = @ReportID;

    SELECT @StrValue;
    EXEC dbo.sp_WriteToFile @StrLocation, @StrValue;
END;
GO

IF OBJECT_ID('dbo.tbReportApprovalReminderDelivery', 'U') IS NOT NULL
    DROP TABLE dbo.tbReportApprovalReminderDelivery;
GO
