[CmdletBinding()]
param(
    [string]$WorkspaceRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$TaskName = 'ER System Daily Web Clean Architecture Audit',
    [string]$DailyTime = '18:00'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$auditScript = Join-Path $WorkspaceRoot 'tools\Invoke-WebCleanArchitectureAudit.ps1'
if (-not (Test-Path -LiteralPath $auditScript)) {
    throw "The audit script was not found at '$auditScript'."
}

$time = [TimeSpan]::ParseExact(
    $DailyTime,
    'hh\:mm',
    [System.Globalization.CultureInfo]::InvariantCulture)
$firstRun = (Get-Date).Date.Add($time)

$powerShellPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$arguments = '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "{0}" -WorkspaceRoot "{1}"' -f `
    $auditScript, $WorkspaceRoot
$action = New-ScheduledTaskAction `
    -Execute $powerShellPath `
    -Argument $arguments `
    -WorkingDirectory $WorkspaceRoot
$trigger = New-ScheduledTaskTrigger -Daily -At $firstRun
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2)
$principal = New-ScheduledTaskPrincipal `
    -UserId ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name) `
    -LogonType Interactive `
    -RunLevel Limited

Register-ScheduledTask `
    -TaskName $TaskName `
    -Description 'Daily report-only Clean Architecture audit for the ER System Web projects.' `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Force | Out-Null

$task = Get-ScheduledTask -TaskName $TaskName
$taskInfo = Get-ScheduledTaskInfo -TaskName $TaskName

Write-Output ('Task: {0}' -f $task.TaskName)
Write-Output ('State: {0}' -f $task.State)
Write-Output ('Next run: {0}' -f $taskInfo.NextRunTime)
Write-Output ('Schedule: Daily at {0} (Windows local time / Asia-Manila)' -f $DailyTime)
