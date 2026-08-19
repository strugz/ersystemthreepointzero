[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$OutputPath = (Join-Path $env:LOCALAPPDATA 'ERSystem\ApprovalReminders\publish')
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'Web\Backend\src\ERSystem.Reminders.Worker\ERSystem.Reminders.Worker.csproj'
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Reminder worker project was not found at $projectPath"
}

New-Item -ItemType Directory -Path $resolvedOutputPath -Force | Out-Null

& dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $resolvedOutputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$executablePath = Join-Path $resolvedOutputPath 'ERSystem.Reminders.Worker.exe'
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Publish completed without the expected executable: $executablePath"
}

Write-Host "Reminder service published to $resolvedOutputPath"
Write-Host "Executable: $executablePath"
Write-Host 'Keep production SQL, SMTP, and SMS settings in a protected JSON file outside this publish directory.'
