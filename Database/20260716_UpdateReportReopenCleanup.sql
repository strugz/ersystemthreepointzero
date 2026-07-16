SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.sp2_DeleteVar', N'P') IS NULL
BEGIN
    RAISERROR('dbo.sp2_DeleteVar does not exist.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.tbReportApprovalTransaction', N'U') IS NULL
BEGIN
    RAISERROR('dbo.tbReportApprovalTransaction does not exist. Apply the web approval support scripts first.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.tbReportFinanceTracking', N'U') IS NULL
BEGIN
    RAISERROR('dbo.tbReportFinanceTracking does not exist. Apply the Finance tracking support scripts first.', 16, 1);
    RETURN;
END;
GO

ALTER PROCEDURE dbo.sp2_DeleteVar
    @userID int,
    @Image varbinary(max),
    @reportID varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ReportFileStatus varchar(10);
        DECLARE @ReportPrintStatus varchar(10);
        DECLARE @PreviousState varchar(100);
        DECLARE @UserFullName varchar(255);
        DECLARE @HasApprovalTracking bit;
        DECLARE @HasFinanceTracking bit;

        SELECT
            @ReportFileStatus = ReportFileStatus,
            @ReportPrintStatus = ReportPrintStatus
        FROM dbo.tbReportDetails WITH (UPDLOCK, HOLDLOCK)
        WHERE ID = @reportID
          AND UserID = @userID;

        IF @ReportFileStatus IS NULL
        BEGIN
            RAISERROR('The expense report was not found or does not belong to the current user.', 16, 1);
        END;

        SET @HasApprovalTracking = CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.tbReportApprovalTransaction
            WHERE ReportID = @reportID
        ) THEN 1 ELSE 0 END;

        SET @HasFinanceTracking = CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.tbReportFinanceTracking
            WHERE ReportID = @reportID
        ) THEN 1 ELSE 0 END;

        IF NOT
        (
            @ReportFileStatus = '1'
            OR (@ReportFileStatus = '0' AND @ReportPrintStatus = '0')
            -- Older desktop clients reset the report header immediately before calling this procedure.
            OR (@ReportFileStatus = '0' AND @ReportPrintStatus = '1'
                AND (@HasApprovalTracking = 1 OR @HasFinanceTracking = 1))
        )
        BEGIN
            RAISERROR('Only reports that are for approval or approved can be reopened for editing.', 16, 1);
        END;

        SET @PreviousState = CASE
            WHEN @ReportFileStatus = '1' THEN 'For Approval'
            WHEN @ReportPrintStatus = '0' OR @HasFinanceTracking = 1 THEN 'Approved'
            ELSE 'For Approval'
        END;

        SELECT @UserFullName = Fullname
        FROM dbo.tbUserRegistration
        WHERE UserID = @userID;

        UPDATE dbo.tbReportDetails
        SET ReportPrintStatus = '1',
            ReportEndorseStatus = 'NOT APPROVED',
            ReportFileStatus = '0',
            ReportNumberStatus = '0',
            ReportReserveStatus1 = NULL,
            ReportReserveStatus2 = NULL,
            ReportSentStatus = '0',
            ReportEndorseSignature = @Image,
            ReportReserveSignature = @Image,
            ReportCancelNote = 'Cancelled by ' + ISNULL(@UserFullName, CONVERT(varchar(20), @userID))
        WHERE ID = @reportID
          AND UserID = @userID;

        DELETE FROM dbo.tbReportAuthority
        WHERE ReportID = @reportID;

        DELETE FROM dbo.tbReportApprovalTransaction
        WHERE ReportID = @reportID;

        DELETE FROM dbo.tbReportFinanceTracking
        WHERE ReportID = @reportID;

        IF OBJECT_ID(N'dbo.tbWebWorkflowAudit', N'U') IS NOT NULL
        BEGIN
            INSERT INTO dbo.tbWebWorkflowAudit
                (ReportID, ActorUserID, EventType, PreviousState, NewState, Remarks, OccurredAtUtc, CorrelationID)
            VALUES
                (@reportID, @userID, 'ReportReopened', @PreviousState, 'New',
                 N'Approval and Finance tracking were cleared when the report was reopened for editing.',
                 GETUTCDATE(), NEWID());
        END;

        EXEC dbo.sp_Notify @reportID, 'CANCEL';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage nvarchar(4000);
        DECLARE @ErrorSeverity int;
        DECLARE @ErrorState int;

        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
GO
