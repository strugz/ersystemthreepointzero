IF COL_LENGTH('dbo.tbReportDetails', 'ReportAttachment') IS NOT NULL
BEGIN
    ALTER TABLE dbo.tbReportDetails
        ALTER COLUMN ReportAttachment varchar(max) NULL;
END
GO
