IF COL_LENGTH('dbo.tbReportDetails', 'ReportType') IS NULL
BEGIN
    ALTER TABLE dbo.tbReportDetails
        ADD ReportType varchar(50) NULL;
END
