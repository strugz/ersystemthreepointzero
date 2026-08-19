/*
    Read-only verification for durable scanned receipt storage.
    This script does not update or delete application data.
*/
SET NOCOUNT ON;

SELECT
    COUNT_BIG(*) AS AttachmentCount,
    ISNULL(SUM(CONVERT(bigint, DATALENGTH(receipt.ReceiptContent))), 0) AS StoredContentBytes,
    ISNULL(SUM(CASE WHEN report.ID IS NULL THEN 1 ELSE 0 END), 0) AS OrphanedAttachmentCount,
    ISNULL(SUM(CASE WHEN receipt.ReceiptContent IS NULL OR DATALENGTH(receipt.ReceiptContent) = 0 THEN 1 ELSE 0 END), 0) AS EmptyContentCount,
    ISNULL(SUM(CASE WHEN CONVERT(bigint, DATALENGTH(receipt.ReceiptContent)) <> receipt.FileSizeBytes THEN 1 ELSE 0 END), 0) AS SizeMismatchCount,
    ISNULL(SUM(CASE
            WHEN LOWER(receipt.FileExtension) = '.pdf' AND LOWER(receipt.ContentType) = 'application/pdf' THEN 0
            WHEN LOWER(receipt.FileExtension) IN ('.jpg', '.jpeg') AND LOWER(receipt.ContentType) = 'image/jpeg' THEN 0
            WHEN LOWER(receipt.FileExtension) = '.png' AND LOWER(receipt.ContentType) = 'image/png' THEN 0
            ELSE 1
        END), 0) AS UnsupportedOrMismatchedTypeCount
FROM dbo.tbScannedReceiptAttachment AS receipt
LEFT JOIN dbo.tbReportDetails AS report ON report.ID = receipt.ReportID;

SELECT
    CASE
        WHEN receipt.FileSizeBytes < 1048576 THEN '< 1 MB'
        WHEN receipt.FileSizeBytes < 10485760 THEN '1-10 MB'
        WHEN receipt.FileSizeBytes < 52428800 THEN '10-50 MB'
        ELSE '>= 50 MB'
    END AS SizeBand,
    COUNT_BIG(*) AS AttachmentCount,
    SUM(receipt.FileSizeBytes) AS TotalBytes
FROM dbo.tbScannedReceiptAttachment AS receipt
GROUP BY
    CASE
        WHEN receipt.FileSizeBytes < 1048576 THEN '< 1 MB'
        WHEN receipt.FileSizeBytes < 10485760 THEN '1-10 MB'
        WHEN receipt.FileSizeBytes < 52428800 THEN '10-50 MB'
        ELSE '>= 50 MB'
    END
ORDER BY MIN(receipt.FileSizeBytes);

SELECT
    report.ID,
    report.ERFReferenceNo,
    report.ReportEndorseStatus,
    report.ReportFileStatus,
    report.ReportPrintStatus,
    COUNT(receipt.ID) AS RetainedAttachmentCount,
    SUM(CONVERT(bigint, DATALENGTH(receipt.ReceiptContent))) AS RetainedContentBytes
FROM dbo.tbReportDetails AS report
LEFT JOIN dbo.tbScannedReceiptAttachment AS receipt ON receipt.ReportID = report.ID
WHERE report.ReportEndorseStatus = 'APPROVED'
  AND report.ReportFileStatus = '0'
  AND report.ReportPrintStatus = '0'
  AND ISNULL(report.ReportAttachment, '') = ''
GROUP BY
    report.ID,
    report.ERFReferenceNo,
    report.ReportEndorseStatus,
    report.ReportFileStatus,
    report.ReportPrintStatus
ORDER BY report.ERFReferenceNo;
