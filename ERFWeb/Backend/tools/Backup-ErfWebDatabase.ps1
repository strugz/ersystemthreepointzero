<#
.SYNOPSIS
    Nightly backup of the ERFWeb PostgreSQL database (plan Phase 8.6).

.DESCRIPTION
    Run on the API server by the "ERFWeb Database Backup" scheduled task (Install-ErfWebMaintenance.ps1), or by
    hand. It:
      1. reads ConnectionStrings:Postgres from the server's appsettings.json (the password is passed to
         pg_dump through PGPASSWORD for that one process and never written anywhere);
      2. runs pg_dump in custom format to <BackupDir>\<database>_yyyyMMdd_HHmm.dump (written as .partial first,
         so a failed run never leaves a file that looks complete);
      3. checks the file: pg_restore must read the whole archive and find table data, including workflow_audit;
      4. copies it to -CopyTo (the network share) and checks the copy's size;
      5. deletes backups older than -KeepDays in both places (only files this script made);
      6. logs every step to <BackupDir>\backup-log.txt and emails -AlertTo when anything fails.
    PostgreSQL runs on another server, so this server needs the PostgreSQL client tools, of a version at least
    as new as the database server's (pg_dump refuses to dump a newer server).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Backup-ErfWebDatabase.ps1 -CopyTo \\fileserver\backups\erfweb
#>
[CmdletBinding()]
param(
    [string] $InstallPath = 'C:\inetpub\wwwroot\erf_web-app',
    [string] $BackupDir = 'D:\ERFWeb-Backups',
    # Network share for a second copy, e.g. \\fileserver\backups\erfweb. Empty: local copy only.
    [string] $CopyTo,
    [int] $KeepDays = 14,
    [string[]] $AlertTo = @(),
    # Folder with pg_dump.exe and pg_restore.exe; found automatically when empty.
    [string] $PgBin
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ErfWebMaintenance.Common.ps1')

$logFile = Join-Path $BackupDir 'backup-log.txt'
$settings = $null

function Find-PgTool([string] $name) {
    if ($PgBin) {
        $path = Join-Path $PgBin $name
        if (Test-Path $path) { return $path }
        throw "$name not found in $PgBin."
    }
    $onPath = Get-Command $name -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    # Newest installed version first ("C:\Program Files\PostgreSQL\17\bin").
    $found = Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin' -Filter $name -ErrorAction SilentlyContinue |
        Sort-Object { [int](($_.Directory.Parent.Name -split '\.')[0]) } -Descending |
        Select-Object -First 1
    if ($found) { return $found.FullName }
    throw "$name not found. Install the PostgreSQL command line tools on this server, or pass -PgBin."
}

function Remove-OldBackups([string] $dir, [string] $database) {
    if (-not $dir -or -not (Test-Path $dir)) { return }
    $cutoff = (Get-Date).AddDays(-$KeepDays)
    Get-ChildItem -Path $dir -Filter "$($database)_*.dump" -File |
        Where-Object { $_.LastWriteTime -lt $cutoff } |
        ForEach-Object {
            Remove-Item -LiteralPath $_.FullName -Force
            Write-MaintenanceLog -LogFile $logFile -Message "Deleted old backup $($_.Name) from $dir."
        }
}

$partial = $null
try {
    New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
    $settings = Read-ErfWebSettings -InstallPath $InstallPath
    if (-not $settings.ConnectionStrings -or -not $settings.ConnectionStrings.Postgres) {
        throw 'ConnectionStrings:Postgres is missing from appsettings.json.'
    }
    $db = ConvertFrom-NpgsqlConnectionString -ConnectionString $settings.ConnectionStrings.Postgres
    $pgDump = Find-PgTool 'pg_dump.exe'
    $pgRestore = Find-PgTool 'pg_restore.exe'

    $name = "$($db.Database)_$(Get-Date -Format 'yyyyMMdd_HHmm').dump"
    $target = Join-Path $BackupDir $name
    $partial = "$target.partial"
    Write-MaintenanceLog -LogFile $logFile -Message "Backing up $($db.Database) on $($db.Host) to $target."

    # Windows PowerShell 5.1 turns a native tool's stderr line into a terminating error under 'Stop'; the
    # exit code decides instead.
    $ErrorActionPreference = 'Continue'
    $env:PGPASSWORD = $db.Password
    try {
        & $pgDump --host=$($db.Host) --port=$($db.Port) --username=$($db.Username) --dbname=$($db.Database) `
            --format=custom --no-password --file=$partial 2>&1 | ForEach-Object { Write-MaintenanceLog -LogFile $logFile -Message "pg_dump: $_" }
        if ($LASTEXITCODE -ne 0) { throw "pg_dump stopped with exit code $LASTEXITCODE." }
    }
    finally {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }

    # The restore check: pg_restore must read the whole archive's table of contents and find the tables' data.
    $contents = & $pgRestore --list $partial 2>&1
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) { throw "pg_restore could not read the backup (exit code $LASTEXITCODE); the file is damaged." }
    $tableData = @($contents | Where-Object { $_ -match ' TABLE DATA ' })
    if ($tableData.Count -eq 0 -or -not ($tableData | Where-Object { $_ -match ' workflow_audit ' })) {
        throw 'The backup has no table data (or no workflow_audit); it would not restore the system.'
    }

    Move-Item -LiteralPath $partial -Destination $target -Force
    $partial = $null
    $size = (Get-Item -LiteralPath $target).Length
    Write-MaintenanceLog -LogFile $logFile -Message ("Backup OK: {0} ({1:N1} MB, {2} tables with data)." -f $name, ($size / 1MB), $tableData.Count)

    if ($CopyTo) {
        New-Item -ItemType Directory -Force -Path $CopyTo | Out-Null
        $copy = Join-Path $CopyTo $name
        Copy-Item -LiteralPath $target -Destination $copy -Force
        if ((Get-Item -LiteralPath $copy).Length -ne $size) { throw "The copy on $CopyTo is not the same size as the backup." }
        Write-MaintenanceLog -LogFile $logFile -Message "Copied to $CopyTo."
    }

    Remove-OldBackups $BackupDir $db.Database
    Remove-OldBackups $CopyTo $db.Database
}
catch {
    $reason = $_.Exception.Message
    Write-MaintenanceLog -LogFile $logFile -Message "BACKUP FAILED: $reason"
    if ($partial -and (Test-Path -LiteralPath $partial)) { Remove-Item -LiteralPath $partial -Force -ErrorAction SilentlyContinue }
    if ($settings -and $AlertTo.Count -gt 0) {
        Send-ErfWebAlert -Settings $settings -To $AlertTo -LogFile $logFile -Subject 'Database backup FAILED' `
            -Body "Tonight's ERFWeb database backup failed:`r`n`r`n$reason`r`n`r`nSee $logFile on the server."
    }
    exit 1
}
