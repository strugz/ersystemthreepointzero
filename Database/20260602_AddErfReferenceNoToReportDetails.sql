IF COL_LENGTH('dbo.tbReportDetails', 'ERFReferenceNo') IS NULL
BEGIN
    ALTER TABLE dbo.tbReportDetails
        ADD ERFReferenceNo varchar(50) NULL;
END;
