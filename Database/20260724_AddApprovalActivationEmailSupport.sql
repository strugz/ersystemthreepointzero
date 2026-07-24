SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tbReportApprovalReminderDelivery', 'U') IS NULL
BEGIN
    RAISERROR('Apply 20260720_CreateApprovalReminderSupport.sql before this script.', 16, 1);
    RETURN;
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

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
        CHECK (ReminderNumber >= 0);

    ALTER TABLE dbo.tbReportApprovalReminderDelivery
        CHECK CONSTRAINT CK_tbReportApprovalReminderDelivery_ReminderNumber;

    /*
        Existing actionable approvals are marked as preexisting so deploying the
        activation poller cannot send a mass batch of "just filed" email.
    */
    ;WITH ActionableApprovals AS
    (
        SELECT currentStep.ID,
               currentStep.ReportID,
               currentStep.ApprovalCycle,
               currentStep.EmployeeUserID,
               currentStep.ApproverUserID
        FROM dbo.tbReportApprovalTransaction currentStep
        INNER JOIN dbo.tbReportDetails report
            ON report.ID = currentStep.ReportID
        WHERE currentStep.Status = 'Pending'
          AND report.ReportFileStatus = '1'
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
    INSERT INTO dbo.tbReportApprovalReminderDelivery
    (
        ApprovalTransactionID,
        ReportID,
        ApprovalCycle,
        ReminderNumber,
        Channel,
        Audience,
        RecipientUserID,
        DeliveryStatus,
        FailureCode,
        CorrelationID,
        CreatedAtUtc,
        AttemptedAtUtc,
        CompletedAtUtc
    )
    SELECT actionable.ID,
           actionable.ReportID,
           actionable.ApprovalCycle,
           0,
           'Email',
           recipient.Audience,
           recipient.RecipientUserID,
           'Skipped',
           'ACTIVATION_PREEXISTING',
           NEWID(),
           GETUTCDATE(),
           GETUTCDATE(),
           GETUTCDATE()
    FROM ActionableApprovals actionable
    CROSS APPLY
    (
        SELECT 'Manager' AS Audience,
               actionable.ApproverUserID AS RecipientUserID
        UNION ALL
        SELECT 'Employee',
               actionable.EmployeeUserID
    ) recipient
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tbReportApprovalReminderDelivery existing
        WHERE existing.ApprovalTransactionID = actionable.ID
          AND existing.ReminderNumber = 0
          AND existing.Channel = 'Email'
          AND existing.Audience = recipient.Audience
    );

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
