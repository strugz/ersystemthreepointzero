IF OBJECT_ID('dbo.tbScannedReceiptAttachment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbScannedReceiptAttachment
    (
        ID int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_tbScannedReceiptAttachment PRIMARY KEY,
        ReportID varchar(50) NOT NULL,
        OriginalFileName nvarchar(260) NOT NULL,
        StoredFilePath nvarchar(500) NULL,
        ContentType nvarchar(100) NOT NULL,
        FileExtension nvarchar(20) NOT NULL,
        FileSizeBytes bigint NOT NULL,
        ReceiptContent varbinary(max) NOT NULL,
        CreatedByUserID int NULL,
        CreatedDate datetime NOT NULL
            CONSTRAINT DF_tbScannedReceiptAttachment_CreatedDate DEFAULT (GETDATE()),
        CONSTRAINT FK_tbScannedReceiptAttachment_tbReportDetails
            FOREIGN KEY (ReportID) REFERENCES dbo.tbReportDetails(ID)
    );

    CREATE INDEX IX_tbScannedReceiptAttachment_ReportID
        ON dbo.tbScannedReceiptAttachment(ReportID);
END
GO
