/*
    Restores the previous dbo.sp2_DeleteVar behavior.

    This rollback cannot restore approval or Finance tracking rows that were
    already deleted by the forward script. Restore those records from a reviewed
    database backup if recovery is required.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.sp2_DeleteVar', N'P') IS NULL
BEGIN
    RAISERROR('dbo.sp2_DeleteVar does not exist.', 16, 1);
    RETURN;
END;
GO

ALTER PROCEDURE dbo.sp2_DeleteVar
    @userID int,
    @Image varbinary(max),
    @reportID varchar(50)
AS
BEGIN
    UPDATE dbo.tbReportDetails
    SET ReportEndorseSignature = @Image,
        ReportReserveSignature = @Image,
        ReportReserveStatus1 = NULL,
        ReportReserveStatus2 = NULL,
        ReportNumberStatus = 0,
        ReportSentStatus = 0
    WHERE UserID = @userID
      AND ID = @reportID;

    UPDATE dbo.tbReportDetails
    SET ReportCancelNote = 'Cancelled by ' +
        (SELECT TOP 1 Fullname FROM dbo.tbUserRegistration WHERE UserID = @userID)
    WHERE UserID = @userID
      AND ID = @reportID;

    EXEC dbo.sp_Notify @reportID, 'CANCEL';

    DELETE FROM dbo.tbReportAuthority
    WHERE ReportID = @reportID;
END;
GO
