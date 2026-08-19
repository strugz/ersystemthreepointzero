SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.tbUserRegistration', 'NotificationEmail') IS NULL
BEGIN
    ALTER TABLE dbo.tbUserRegistration
        ADD NotificationEmail nvarchar(320) NULL;
END;
GO

IF OBJECT_ID('dbo.tbReportApprovalReminderDelivery', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbReportApprovalReminderDelivery
    (
        ID bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tbReportApprovalReminderDelivery PRIMARY KEY,
        ApprovalTransactionID bigint NOT NULL,
        ReportID varchar(50) NOT NULL,
        ApprovalCycle int NOT NULL,
        ReminderNumber int NOT NULL,
        Channel varchar(20) NOT NULL,
        Audience varchar(30) NOT NULL,
        RecipientUserID int NULL,
        DeliveryStatus varchar(20) NOT NULL,
        FailureCode varchar(100) NULL,
        CorrelationID uniqueidentifier NOT NULL,
        CreatedAtUtc datetime NOT NULL
            CONSTRAINT DF_tbReportApprovalReminderDelivery_CreatedAtUtc DEFAULT (GETUTCDATE()),
        AttemptedAtUtc datetime NOT NULL
            CONSTRAINT DF_tbReportApprovalReminderDelivery_AttemptedAtUtc DEFAULT (GETUTCDATE()),
        CompletedAtUtc datetime NULL,
        CONSTRAINT FK_tbReportApprovalReminderDelivery_ApprovalTransaction
            FOREIGN KEY (ApprovalTransactionID) REFERENCES dbo.tbReportApprovalTransaction(ID),
        CONSTRAINT CK_tbReportApprovalReminderDelivery_ReminderNumber
            CHECK (ReminderNumber > 0),
        CONSTRAINT CK_tbReportApprovalReminderDelivery_Channel
            CHECK (Channel IN ('Email', 'SmsGateway')),
        CONSTRAINT CK_tbReportApprovalReminderDelivery_Audience
            CHECK (Audience IN ('Manager', 'Employee', 'ManagerAndEmployee')),
        CONSTRAINT CK_tbReportApprovalReminderDelivery_Status
            CHECK (DeliveryStatus IN ('Attempting', 'Queued', 'Sent', 'Failed', 'Skipped')),
        CONSTRAINT UQ_tbReportApprovalReminderDelivery_Occurrence
            UNIQUE (ApprovalTransactionID, ReminderNumber, Channel, Audience)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.tbReportApprovalReminderDelivery')
      AND name = 'IX_tbReportApprovalReminderDelivery_StatusCreated'
)
BEGIN
    CREATE INDEX IX_tbReportApprovalReminderDelivery_StatusCreated
        ON dbo.tbReportApprovalReminderDelivery(DeliveryStatus, CreatedAtUtc);
END;
GO

IF OBJECT_ID('dbo.sp_Notify', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp_Notify AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp_Notify
    @ReportID varchar(100),
    @Status varchar(20) = 'APPROVE',
    @ReminderApproverUsername varchar(50) = NULL,
    @ReminderMessage varchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StrValue varchar(2000);
    DECLARE @StrLocation varchar(2000);
    DECLARE @SafeReminderMessage varchar(320);

    IF @Status = 'REMINDER'
    BEGIN
        IF NULLIF(LTRIM(RTRIM(@ReminderApproverUsername)), '') IS NULL
           OR NULLIF(LTRIM(RTRIM(@ReminderMessage)), '') IS NULL
        BEGIN
            RAISERROR('A reminder manager and message are required.', 16, 1);
            RETURN;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.tbReportApprovalTransaction currentStep
            INNER JOIN dbo.tbReportDetails report
                ON report.ID = currentStep.ReportID
            INNER JOIN dbo.tbUserRegistration managerAccount
                ON managerAccount.UserID = currentStep.ApproverUserID
            INNER JOIN dbo.tbUserRegistration employeeAccount
                ON employeeAccount.UserID = currentStep.EmployeeUserID
            WHERE currentStep.ReportID = @ReportID
              AND currentStep.Status = 'Pending'
              AND report.ReportFileStatus = '1'
              AND report.UserID = currentStep.EmployeeUserID
              AND managerAccount.username = @ReminderApproverUsername
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.tbReportApprovalTransaction earlierStep
                  WHERE earlierStep.ReportID = currentStep.ReportID
                    AND earlierStep.ApprovalCycle = currentStep.ApprovalCycle
                    AND earlierStep.StepOrder < currentStep.StepOrder
                    AND earlierStep.Status <> 'Approved'
              )
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.tbReportApprovalTransaction laterCycle
                  WHERE laterCycle.ReportID = currentStep.ReportID
                    AND laterCycle.ApprovalCycle > currentStep.ApprovalCycle
              )
        )
        BEGIN
            RAISERROR('The report is no longer actionable by the supplied manager.', 16, 1);
            RETURN;
        END;

        SELECT @SafeReminderMessage = LEFT(
            REPLACE(REPLACE(REPLACE(@ReminderMessage, '|', ' '), CHAR(13), ' '), CHAR(10), ' '),
            320);

        SELECT @StrValue =
            (SELECT department.emp_Dept
             FROM dbo.tblDept department
             WHERE department.ID = account.DeptID) + '|REMINDER|' +
            CONVERT(varchar(17), GETDATE()) + '|' +
            ISNULL(report.ReportDateFiled, '') + '||' +
            ISNULL(CONVERT(varchar(1), report.ReportNumberStatus), '') + '|' +
            ISNULL(report.ReportPrintStatus, '') + '|' +
            ISNULL(report.ReportFileStatus, '') + '|' +
            ISNULL(@SafeReminderMessage, '') + '||' +
            ISNULL(account.username, '') + '|' +
            ISNULL(@ReminderApproverUsername, '') + '|'
        FROM dbo.tbReportDetails report
        LEFT JOIN dbo.tbUserRegistration account ON report.UserID = account.UserID
        WHERE report.ID = @ReportID;
    END
    ELSE
    BEGIN
        /* Preserve the deployed FILE, DONE, CANCEL, and default payload exactly. */
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
    END;

    SELECT @StrLocation = 'D:\ERSHARE\' + RIGHT(NEWID(), 12) + '.txt';
    SELECT @StrValue;
    EXEC dbo.sp_WriteToFile @StrLocation, @StrValue;
END;
GO
