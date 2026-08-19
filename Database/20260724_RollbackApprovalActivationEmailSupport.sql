SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tbReportApprovalReminderDelivery', 'U') IS NULL
    RETURN;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE FROM dbo.tbReportApprovalReminderDelivery
    WHERE ReminderNumber = 0;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.tbReportApprovalReminderDelivery')
          AND name = 'CK_tbReportApprovalReminderDelivery_ReminderNumber'
    )
    BEGIN
        ALTER TABLE dbo.tbReportApprovalReminderDelivery
            DROP CONSTRAINT CK_tbReportApprovalReminderDelivery_ReminderNumber;
    END;

    ALTER TABLE dbo.tbReportApprovalReminderDelivery WITH CHECK
        ADD CONSTRAINT CK_tbReportApprovalReminderDelivery_ReminderNumber
        CHECK (ReminderNumber > 0);

    ALTER TABLE dbo.tbReportApprovalReminderDelivery
        CHECK CONSTRAINT CK_tbReportApprovalReminderDelivery_ReminderNumber;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage nvarchar(4000);
    SELECT @ErrorMessage = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;
GO
