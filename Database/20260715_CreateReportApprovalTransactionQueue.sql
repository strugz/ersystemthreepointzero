SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.tbReportApprovalTransaction', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbReportApprovalTransaction
    (
        ID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbReportApprovalTransaction PRIMARY KEY,
        ReportID varchar(50) NOT NULL,
        ApprovalCycle int NOT NULL,
        EmployeeUserID int NOT NULL,
        ApproverUserID int NOT NULL,
        StepOrder int NOT NULL,
        Status varchar(20) NOT NULL CONSTRAINT DF_tbReportApprovalTransaction_Status DEFAULT ('Pending'),
        SubmittedAtUtc datetime NOT NULL CONSTRAINT DF_tbReportApprovalTransaction_SubmittedAtUtc DEFAULT (GETUTCDATE()),
        ActionedAtUtc datetime NULL,
        ActionRemarks nvarchar(1000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_tbReportApprovalTransaction_tbReportDetails
            FOREIGN KEY (ReportID) REFERENCES dbo.tbReportDetails(ID),
        CONSTRAINT CK_tbReportApprovalTransaction_Status
            CHECK (Status IN ('Pending', 'Approved', 'Returned', 'Superseded')),
        CONSTRAINT UQ_tbReportApprovalTransaction_ReportCycleApprover
            UNIQUE (ReportID, ApprovalCycle, ApproverUserID),
        CONSTRAINT UQ_tbReportApprovalTransaction_ReportCycleStep
            UNIQUE (ReportID, ApprovalCycle, StepOrder)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportApprovalTransaction') AND name = 'IX_tbReportApprovalTransaction_ManagerQueue')
    CREATE INDEX IX_tbReportApprovalTransaction_ManagerQueue
        ON dbo.tbReportApprovalTransaction(ApproverUserID, Status, StepOrder, ReportID, ApprovalCycle);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tbReportApprovalTransaction') AND name = 'IX_tbReportApprovalTransaction_ReportCycle')
    CREATE INDEX IX_tbReportApprovalTransaction_ReportCycle
        ON dbo.tbReportApprovalTransaction(ReportID, ApprovalCycle, Status, StepOrder);
GO

/* Deliberately no backfill: only reports filed after this migration enter the manager queue. */

IF OBJECT_ID('dbo.sp2_RefileER', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp2_RefileER AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp2_RefileER
    @ReportID varchar(50),
    @status int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @EmployeeUserID int;
        DECLARE @ApprovalCycle int;
        DECLARE @CurrentFileStatus varchar(1);

        SELECT @EmployeeUserID = UserID, @CurrentFileStatus = ReportFileStatus
        FROM dbo.tbReportDetails WITH (UPDLOCK, HOLDLOCK)
        WHERE ID = @ReportID;

        IF @EmployeeUserID IS NULL
        BEGIN
            RAISERROR('The report or report owner was not found.', 16, 1);
        END;

        IF @CurrentFileStatus = '1' AND EXISTS
        (
            SELECT 1 FROM dbo.tbReportApprovalTransaction
            WHERE ReportID = @ReportID AND Status = 'Pending'
              AND ApprovalCycle = (SELECT MAX(ApprovalCycle) FROM dbo.tbReportApprovalTransaction WHERE ReportID = @ReportID)
        )
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.tbUserAuthority
            WHERE UserID = @EmployeeUserID AND AuthorityID IS NOT NULL AND Sort IS NOT NULL
        )
        BEGIN
            RAISERROR('The report owner has no valid approval assignments.', 16, 1);
        END;

        IF EXISTS
        (
            SELECT AuthorityID FROM dbo.tbUserAuthority
            WHERE UserID = @EmployeeUserID AND AuthorityID IS NOT NULL AND Sort IS NOT NULL
            GROUP BY AuthorityID HAVING COUNT(*) > 1
        ) OR EXISTS
        (
            SELECT Sort FROM dbo.tbUserAuthority
            WHERE UserID = @EmployeeUserID AND AuthorityID IS NOT NULL AND Sort IS NOT NULL
            GROUP BY Sort HAVING COUNT(*) > 1
        )
        BEGIN
            RAISERROR('The report owner has duplicate approver or approval-step assignments.', 16, 1);
        END;

        UPDATE dbo.tbReportApprovalTransaction
        SET Status = 'Superseded', ActionedAtUtc = GETUTCDATE()
        WHERE ReportID = @ReportID AND Status = 'Pending';

        SELECT @ApprovalCycle = ISNULL(MAX(ApprovalCycle), 0) + 1
        FROM dbo.tbReportApprovalTransaction WITH (UPDLOCK, HOLDLOCK)
        WHERE ReportID = @ReportID;

        UPDATE dbo.tbReportDetails
        SET ReportFileStatus = CONVERT(varchar(1), @status),
            ReportPrintStatus = '1',
            ReportCancelNote = '',
            ReportNumberStatus = 0,
            ReportReserveStatus1 = NULL,
            ReportReserveStatus2 = NULL
        WHERE ID = @ReportID;

        INSERT INTO dbo.tbReportApprovalTransaction
            (ReportID, ApprovalCycle, EmployeeUserID, ApproverUserID, StepOrder, Status, SubmittedAtUtc)
        SELECT @ReportID, @ApprovalCycle, @EmployeeUserID, CONVERT(int, AuthorityID), CONVERT(int, Sort), 'Pending', GETUTCDATE()
        FROM dbo.tbUserAuthority
        WHERE UserID = @EmployeeUserID AND AuthorityID IS NOT NULL AND Sort IS NOT NULL;

        EXEC dbo.sp_Notify @ReportID, 'FILE';
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000);
        SELECT @ErrorMessage = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.sp2_LoadUserAccFiled', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp2_LoadUserAccFiled AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp2_LoadUserAccFiled
    @DeptID bigint,
    @SignID bigint
AS
BEGIN
    SET NOCOUNT ON;

    SELECT users.UserID, users.Fullname, COUNT(DISTINCT queue.ReportID) AS [Number of file]
    FROM dbo.tbReportApprovalTransaction queue
    INNER JOIN dbo.tbUserRegistration users ON users.UserID = queue.EmployeeUserID
    INNER JOIN dbo.tbReportDetails report ON report.ID = queue.ReportID
    WHERE queue.ApproverUserID = @SignID
      AND queue.Status = 'Pending'
      AND report.ReportFileStatus = '1'
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.tbReportApprovalTransaction previous
          WHERE previous.ReportID = queue.ReportID
            AND previous.ApprovalCycle = queue.ApprovalCycle
            AND previous.StepOrder < queue.StepOrder
            AND previous.Status <> 'Approved'
      )
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.tbReportApprovalTransaction laterCycle
          WHERE laterCycle.ReportID = queue.ReportID
            AND laterCycle.ApprovalCycle > queue.ApprovalCycle
      )
    GROUP BY users.UserID, users.Fullname;
END;
GO

IF OBJECT_ID('dbo.sp2_LoadUserReportDetailsFILED', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp2_LoadUserReportDetailsFILED AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp2_LoadUserReportDetailsFILED
    @userID varchar(5),
    @FileStatus varchar(10),
    @signID int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT report.ReportDateFrom AS [Date From], report.ReportDateTo AS [Date To],
           report.ReportDescription AS [Report Description], report.ID
    FROM dbo.tbReportApprovalTransaction queue
    INNER JOIN dbo.tbReportDetails report ON report.ID = queue.ReportID
    WHERE queue.EmployeeUserID = CONVERT(int, @userID)
      AND queue.ApproverUserID = @signID
      AND queue.Status = 'Pending'
      AND report.ReportFileStatus = '1'
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.tbReportApprovalTransaction previous
          WHERE previous.ReportID = queue.ReportID
            AND previous.ApprovalCycle = queue.ApprovalCycle
            AND previous.StepOrder < queue.StepOrder
            AND previous.Status <> 'Approved'
      )
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.tbReportApprovalTransaction laterCycle
          WHERE laterCycle.ReportID = queue.ReportID
            AND laterCycle.ApprovalCycle > queue.ApprovalCycle
      )
    ORDER BY report.ReportDateFrom ASC;
END;
GO

IF OBJECT_ID('dbo.sp2_LoadUserReportDetailsDONE', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp2_LoadUserReportDetailsDONE AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp2_LoadUserReportDetailsDONE
    @userID varchar(5),
    @FileStatus varchar(10),
    @signID int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT report.ReportDateFrom AS [Date From], report.ReportDateTo AS [Date To],
           report.ReportDescription AS [Report Description], report.ID
    FROM dbo.tbReportApprovalTransaction queue
    INNER JOIN dbo.tbReportDetails report ON report.ID = queue.ReportID
    WHERE queue.EmployeeUserID = CONVERT(int, @userID)
      AND queue.ApproverUserID = @signID
      AND queue.Status IN ('Approved', 'Returned')
    ORDER BY report.ReportDateFrom ASC;
END;
GO

IF OBJECT_ID('dbo.sp2_UpdateReportNumberStatus', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp2_UpdateReportNumberStatus AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp2_UpdateReportNumberStatus
    @UserID int,
    @ReportID varchar(50),
    @SignID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @TransactionID bigint;
        DECLARE @ApprovalCycle int;
        DECLARE @StepOrder int;
        DECLARE @IsFinal bit;
        DECLARE @ReportNumberStatus int;

        SELECT TOP 1 @TransactionID = ID, @ApprovalCycle = ApprovalCycle, @StepOrder = StepOrder
        FROM dbo.tbReportApprovalTransaction WITH (UPDLOCK, HOLDLOCK)
        WHERE ReportID = @ReportID AND EmployeeUserID = @UserID
          AND ApproverUserID = @SignID AND Status = 'Pending'
        ORDER BY ApprovalCycle DESC;

        IF @TransactionID IS NULL
            RAISERROR('No pending approval transaction is assigned to this manager.', 16, 1);

        IF EXISTS
        (
            SELECT 1 FROM dbo.tbReportApprovalTransaction
            WHERE ReportID = @ReportID AND ApprovalCycle = @ApprovalCycle
              AND StepOrder < @StepOrder AND Status <> 'Approved'
        )
            RAISERROR('A previous approver must complete the report first.', 16, 1);

        SET @IsFinal = CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.tbReportApprovalTransaction
            WHERE ReportID = @ReportID AND ApprovalCycle = @ApprovalCycle AND StepOrder > @StepOrder
        ) THEN 0 ELSE 1 END;

        SELECT @ReportNumberStatus = ISNULL(ReportNumberStatus, 0) + 1
        FROM dbo.tbReportDetails WITH (UPDLOCK, HOLDLOCK)
        WHERE ID = @ReportID AND UserID = @UserID AND ReportFileStatus = '1';

        IF @ReportNumberStatus IS NULL
            RAISERROR('The report is no longer pending approval.', 16, 1);

        UPDATE dbo.tbReportApprovalTransaction
        SET Status = 'Approved', ActionedAtUtc = GETUTCDATE(), ActionRemarks = NULL
        WHERE ID = @TransactionID;

        IF @IsFinal = 0
        BEGIN
            UPDATE dbo.tbReportDetails
            SET ReportPrintStatus = '1', ReportFileStatus = '1', ReportNumberStatus = @ReportNumberStatus,
                ReportReserveStatus1 = CONVERT(varchar(10), @SignID), ReportEndorseStatus = 'APPROVED'
            WHERE ID = @ReportID AND UserID = @UserID;

            IF NOT EXISTS (SELECT 1 FROM dbo.tbReportAuthority WHERE ReportID = @ReportID AND SignID = @SignID)
                INSERT INTO dbo.tbReportAuthority(ReportID, SignID, UserID, AuthoritySignature)
                SELECT @ReportID, @SignID, @UserID, [Signature]
                FROM dbo.tbUserRegistration WHERE UserID = @SignID;

            EXEC dbo.sp_Notify @ReportID, 'FILE';
        END
        ELSE
        BEGIN
            UPDATE dbo.tbReportDetails
            SET ReportPrintStatus = '0', ReportFileStatus = '0', ReportNumberStatus = @ReportNumberStatus,
                ReportReserveStatus2 = CONVERT(varchar(10), @SignID), ReportEndorseStatus = 'APPROVED'
            WHERE ID = @ReportID AND UserID = @UserID;

            IF NOT EXISTS (SELECT 1 FROM dbo.tbReportFinanceTracking WHERE ReportID = @ReportID)
                INSERT INTO dbo.tbReportFinanceTracking(ReportID, FinanceStatus, PhysicalReceiptsReceived, ScannedReceiptsDeletedDate)
                VALUES(@ReportID, 'Pending', 0, GETDATE());
            ELSE
                UPDATE dbo.tbReportFinanceTracking
                SET ScannedReceiptsDeletedDate = ISNULL(ScannedReceiptsDeletedDate, GETDATE())
                WHERE ReportID = @ReportID;

            EXEC dbo.sp_Notify @ReportID, 'DONE';
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000);
        SELECT @ErrorMessage = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.sp2_LoadUserReportDetailsCancel', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp2_LoadUserReportDetailsCancel AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp2_LoadUserReportDetailsCancel
    @reportID varchar(50),
    @reportCancelNote varchar(255),
    @SignID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @TransactionID bigint;
        DECLARE @ApprovalCycle int;
        DECLARE @StepOrder int;

        SELECT TOP 1 @TransactionID = ID, @ApprovalCycle = ApprovalCycle, @StepOrder = StepOrder
        FROM dbo.tbReportApprovalTransaction WITH (UPDLOCK, HOLDLOCK)
        WHERE ReportID = @reportID AND ApproverUserID = @SignID AND Status = 'Pending'
        ORDER BY ApprovalCycle DESC;

        IF @TransactionID IS NULL
            RAISERROR('No pending approval transaction is assigned to this manager.', 16, 1);

        IF EXISTS
        (
            SELECT 1 FROM dbo.tbReportApprovalTransaction
            WHERE ReportID = @reportID AND ApprovalCycle = @ApprovalCycle
              AND StepOrder < @StepOrder AND Status <> 'Approved'
        )
            RAISERROR('A previous approver must complete the report first.', 16, 1);

        UPDATE dbo.tbReportApprovalTransaction
        SET Status = 'Returned', ActionedAtUtc = GETUTCDATE(), ActionRemarks = @reportCancelNote
        WHERE ID = @TransactionID;

        UPDATE dbo.tbReportApprovalTransaction
        SET Status = 'Superseded', ActionedAtUtc = GETUTCDATE()
        WHERE ReportID = @reportID AND ApprovalCycle = @ApprovalCycle
          AND StepOrder > @StepOrder AND Status = 'Pending';

        UPDATE dbo.tbReportDetails
        SET ReportFileStatus = '0', ReportPrintStatus = '1', ReportCancelNote = @reportCancelNote,
            ReportReserveStatus1 = '', ReportNumberStatus = 0, ReportEndorseStatus = 'NOT APPROVED'
        WHERE ID = @reportID;

        EXEC dbo.sp_Notify @reportID, 'CANCEL';
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage nvarchar(4000);
        SELECT @ErrorMessage = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END;
GO
