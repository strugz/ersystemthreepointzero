[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,

    [Parameter(Mandatory = $true)]
    [string]$ReceiptsRoot,

    [string]$ErfReferenceNo
)

$ErrorActionPreference = 'Stop'

$resolvedConfigPath = (Resolve-Path -LiteralPath $ConfigPath).Path
$resolvedReceiptsRoot = if (Test-Path -LiteralPath $ReceiptsRoot) {
    (Resolve-Path -LiteralPath $ReceiptsRoot).Path
} else {
    [System.IO.Path]::GetFullPath($ReceiptsRoot)
}

$config = [xml](Get-Content -LiteralPath $resolvedConfigPath -Raw)
$connectionString = ($config.configuration.connectionStrings.add |
    Where-Object { $_.name -eq 'AppDbContext' }).connectionString

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw 'The AppDbContext connection string was not found.'
}

$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$command = $connection.CreateCommand()
$command.CommandText = @'
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

SELECT COUNT_BIG(receipt.ID) AS ReferenceAttachmentCount,
       ISNULL(SUM(CONVERT(bigint, DATALENGTH(receipt.ReceiptContent))), 0) AS ReferenceContentBytes
FROM dbo.tbReportDetails AS report
LEFT JOIN dbo.tbScannedReceiptAttachment AS receipt ON receipt.ReportID = report.ID
WHERE (@reference = '' OR report.ERFReferenceNo = @reference);
'@

[void]$command.Parameters.Add('@reference', [System.Data.SqlDbType]::VarChar, 100)
$command.Parameters['@reference'].Value = if ($ErfReferenceNo) { $ErfReferenceNo } else { '' }

try {
    $connection.Open()
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $reader = $command.ExecuteReader()

    $integrity = New-Object System.Data.DataTable
    $integrity.Load($reader)
    $reference = New-Object System.Data.DataTable
    $reference.Load($reader)
    $stopwatch.Stop()
} finally {
    $connection.Dispose()
}

$localFiles = if (Test-Path -LiteralPath $resolvedReceiptsRoot) {
    @(Get-ChildItem -LiteralPath $resolvedReceiptsRoot -File -Recurse)
} else {
    @()
}

$row = $integrity.Rows[0]
$referenceRow = $reference.Rows[0]
$violationCount = [long]$row.OrphanedAttachmentCount +
    [long]$row.EmptyContentCount +
    [long]$row.SizeMismatchCount +
    [long]$row.UnsupportedOrMismatchedTypeCount

[pscustomobject]@{
    CheckedAt = Get-Date
    DatabaseAttachmentCount = [long]$row.AttachmentCount
    DatabaseStoredContentBytes = [long]$row.StoredContentBytes
    OrphanedAttachmentCount = [long]$row.OrphanedAttachmentCount
    EmptyContentCount = [long]$row.EmptyContentCount
    SizeMismatchCount = [long]$row.SizeMismatchCount
    UnsupportedOrMismatchedTypeCount = [long]$row.UnsupportedOrMismatchedTypeCount
    MetadataQueryMilliseconds = $stopwatch.ElapsedMilliseconds
    LocalReceiptsRoot = $resolvedReceiptsRoot
    LocalFileCount = $localFiles.Count
    LocalFileBytes = [long](($localFiles | Measure-Object -Property Length -Sum).Sum)
    ErfReferenceNo = $ErfReferenceNo
    ReferenceAttachmentCount = [long]$referenceRow.ReferenceAttachmentCount
    ReferenceContentBytes = [long]$referenceRow.ReferenceContentBytes
    Passed = ($violationCount -eq 0)
}

if ($violationCount -ne 0) {
    exit 1
}
