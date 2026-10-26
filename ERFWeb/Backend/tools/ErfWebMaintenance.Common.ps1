<#
.SYNOPSIS
    Shared helpers for the ERFWeb server maintenance scripts (Backup-ErfWebDatabase.ps1, Test-ErfWebHealth.ps1).

.DESCRIPTION
    Dot-sourced, not run. Everything is read from the server's own appsettings.json in the API folder, so the
    database password and the SMTP login live in one place only. Nothing here writes a password, connection
    string or message body to a log. Windows PowerShell 5.1 compatible.
#>

function Read-ErfWebSettings {
    param([Parameter(Mandatory)] [string] $InstallPath)

    $path = Join-Path $InstallPath 'appsettings.json'
    if (-not (Test-Path $path)) { throw "appsettings.json not found in $InstallPath." }
    return Get-Content -Path $path -Raw | ConvertFrom-Json
}

# Splits an Npgsql connection string ("Host=...;Port=...;Database=...;Username=...;Password=...") into the
# parts pg_dump needs. Accepts the usual Npgsql aliases.
function ConvertFrom-NpgsqlConnectionString {
    param([Parameter(Mandatory)] [string] $ConnectionString)

    $parts = @{}
    foreach ($pair in $ConnectionString.Split(';')) {
        $i = $pair.IndexOf('=')
        if ($i -lt 1) { continue }
        $parts[$pair.Substring(0, $i).Trim().ToLowerInvariant()] = $pair.Substring($i + 1).Trim().Trim('"', "'")
    }

    function Pick([string[]] $keys) { foreach ($k in $keys) { if ($parts.ContainsKey($k)) { return $parts[$k] } }; return $null }

    $port = Pick @('port')
    $result = [pscustomobject]@{
        Host     = Pick @('host', 'server')
        Port     = $(if ($port) { $port } else { '5432' })
        Database = Pick @('database', 'db')
        Username = Pick @('username', 'user id', 'userid', 'user')
        Password = Pick @('password', 'pwd')
    }
    if (-not $result.Host -or -not $result.Database -or -not $result.Username) {
        throw 'ConnectionStrings:Postgres in appsettings.json must name Host, Database and Username.'
    }
    return $result
}

# Emails an alert through the Smtp section of appsettings.json (the mailbox the API sends report email from).
# Never throws: a mail problem is written to the log and the caller carries on.
function Send-ErfWebAlert {
    param(
        [Parameter(Mandatory)] $Settings,
        [Parameter(Mandatory)] [string[]] $To,
        [Parameter(Mandatory)] [string] $Subject,
        [Parameter(Mandatory)] [string] $Body,
        [Parameter(Mandatory)] [string] $LogFile
    )

    $smtp = $Settings.Smtp
    if (-not $smtp -or -not $smtp.Host -or -not $smtp.FromAddress) {
        Write-MaintenanceLog -LogFile $LogFile -Message 'Alert not emailed: the Smtp section of appsettings.json is not set up.'
        return
    }

    $client = $null
    $message = $null
    try {
        $port = 587
        if ($smtp.Port) { $port = [int] $smtp.Port }
        $client = New-Object System.Net.Mail.SmtpClient($smtp.Host, $port)
        # System.Net.Mail speaks STARTTLS only; that is what the company mail server uses (Security: StartTls).
        $client.EnableSsl = ($smtp.Security -ne 'None')
        $client.Timeout = 30000
        if ($smtp.Username) {
            $client.Credentials = New-Object System.Net.NetworkCredential($smtp.Username, $smtp.Password)
        }

        $fromName = 'ERFWeb'
        if ($smtp.FromName) { $fromName = $smtp.FromName }
        $message = New-Object System.Net.Mail.MailMessage
        $message.From = New-Object System.Net.Mail.MailAddress($smtp.FromAddress, $fromName)
        foreach ($address in $To) { $message.To.Add($address) }
        $message.Subject = "[ERFWeb] $Subject"
        $message.Body = "$Body`r`n`r`nServer: $env:COMPUTERNAME`r`nTime: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')`r`n`r`nThis is an automated message from the ERFWeb maintenance tasks."
        $client.Send($message)
        Write-MaintenanceLog -LogFile $LogFile -Message "Alert emailed to $($To.Count) address(es): $Subject"
    }
    catch {
        Write-MaintenanceLog -LogFile $LogFile -Message "Alert could not be emailed ($($_.Exception.GetType().Name)): $Subject"
    }
    finally {
        if ($message) { $message.Dispose() }
        if ($client) { $client.Dispose() }
    }
}

function Write-MaintenanceLog {
    param([Parameter(Mandatory)] [string] $LogFile, [Parameter(Mandatory)] [string] $Message)

    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $Message"
    Write-Host $line
    $dir = Split-Path -Parent $LogFile
    if ($dir) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}
