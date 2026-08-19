IF OBJECT_ID('dbo.tbReportFinanceTracking', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbReportFinanceTracking
    (
        ID bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tbReportFinanceTracking PRIMARY KEY,
        ReportID varchar(50) NOT NULL,
        FinanceStatus varchar(20) NOT NULL
            CONSTRAINT DF_tbReportFinanceTracking_FinanceStatus DEFAULT ('Pending'),
        PhysicalReceiptsReceived bit NOT NULL
            CONSTRAINT DF_tbReportFinanceTracking_PhysicalReceiptsReceived DEFAULT (0),
        PhysicalReceiptsReceivedBy int NULL,
        PhysicalReceiptsReceivedDate datetime NULL,
        FinanceRemarks varchar(255) NULL,
        ScannedReceiptsDeletedDate datetime NULL,
        CONSTRAINT FK_tbReportFinanceTracking_tbReportDetails
            FOREIGN KEY (ReportID) REFERENCES dbo.tbReportDetails(ID),
        CONSTRAINT UQ_tbReportFinanceTracking_ReportID UNIQUE (ReportID)
    );
END;
GO

IF COL_LENGTH('dbo.tbReportFinanceTracking', 'FinanceCompletedBy') IS NOT NULL
BEGIN
    ALTER TABLE dbo.tbReportFinanceTracking
        DROP COLUMN FinanceCompletedBy;
END;
GO

IF COL_LENGTH('dbo.tbReportFinanceTracking', 'FinanceCompletedDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.tbReportFinanceTracking
        DROP COLUMN FinanceCompletedDate;
END;
GO
