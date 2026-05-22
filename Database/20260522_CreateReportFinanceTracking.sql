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
        FinanceCompletedBy int NULL,
        FinanceCompletedDate datetime NULL,
        FinanceRemarks varchar(255) NULL,
        ScannedReceiptsDeletedDate datetime NULL,
        CONSTRAINT FK_tbReportFinanceTracking_tbReportDetails
            FOREIGN KEY (ReportID) REFERENCES dbo.tbReportDetails(ID),
        CONSTRAINT UQ_tbReportFinanceTracking_ReportID UNIQUE (ReportID)
    );
END;
GO

INSERT INTO dbo.tbReportFinanceTracking (ReportID)
SELECT report.ID
FROM dbo.tbReportDetails report
WHERE report.ReportFileStatus = '0'
  AND report.ReportPrintStatus = '0'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.tbReportFinanceTracking finance
      WHERE finance.ReportID = report.ID
  );
GO
