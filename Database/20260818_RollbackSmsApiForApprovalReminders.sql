SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.tbReportApprovalReminderDelivery', N'U') IS NULL
BEGIN
    RAISERROR('dbo.tbReportApprovalReminderDelivery does not exist.', 16, 1);
    RETURN;
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.tbReportApprovalReminderDelivery')
          AND name = N'CK_tbReportApprovalReminderDelivery_Channel'
    )
    BEGIN
        ALTER TABLE dbo.tbReportApprovalReminderDelivery
            DROP CONSTRAINT CK_tbReportApprovalReminderDelivery_Channel;
    END;

    /* Collapse per-recipient API history back to the legacy combined occurrence. */
    INSERT INTO dbo.tbReportApprovalReminderDelivery
    (
        ApprovalTransactionID, ReportID, ApprovalCycle, ReminderNumber,
        Channel, Audience, RecipientUserID, DeliveryStatus, FailureCode,
        CorrelationID, CreatedAtUtc, AttemptedAtUtc, CompletedAtUtc
    )
    SELECT ApprovalTransactionID,
           MAX(ReportID),
           MAX(ApprovalCycle),
           ReminderNumber,
           'SmsGateway',
           'ManagerAndEmployee',
           NULL,
           CASE
               WHEN SUM(CASE WHEN DeliveryStatus = 'Failed' THEN 1 ELSE 0 END) > 0 THEN 'Failed'
               WHEN SUM(CASE WHEN DeliveryStatus = 'Attempting' THEN 1 ELSE 0 END) > 0 THEN 'Attempting'
               ELSE 'Queued'
           END,
           MAX(FailureCode),
           NEWID(),
           MIN(CreatedAtUtc),
           MIN(AttemptedAtUtc),
           MAX(CompletedAtUtc)
    FROM dbo.tbReportApprovalReminderDelivery
    WHERE Channel = 'SmsApi'
    GROUP BY ApprovalTransactionID, ReminderNumber;

    DELETE FROM dbo.tbReportApprovalReminderDelivery
    WHERE Channel = 'SmsApi';

    ALTER TABLE dbo.tbReportApprovalReminderDelivery WITH CHECK
        ADD CONSTRAINT CK_tbReportApprovalReminderDelivery_Channel
        CHECK (Channel IN ('Email', 'SmsGateway'));

    ALTER TABLE dbo.tbReportApprovalReminderDelivery
        CHECK CONSTRAINT CK_tbReportApprovalReminderDelivery_Channel;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @MigrationError nvarchar(4000);
    SELECT @MigrationError = ERROR_MESSAGE();
    RAISERROR(@MigrationError, 16, 1);
END CATCH;
GO

/*
    The safe reminder-child deletion in dbo.sp2_DeleteVar is intentionally retained.
    Reintroducing the previous definition would restore a known foreign-key failure.
*/
