<#
.SYNOPSIS
    Builds the ERFWeb API and packages it as one release folder to copy to the server.

.DESCRIPTION
    Run on the developer PC (double-click Publish-ErfWeb.cmd). It publishes ERFWeb.Api (Release) into a clean
    folder, so no stale file from an older build is included, and writes:

        ERFWeb\releases\ERFWeb-Api-<version>-<commit>\
            ERFWeb-Api.zip              the whole API (never appsettings.json; the server keeps its own)
            release.txt                 the version inside, as /api/health reports it
            Deploy-ErfWeb.cmd           double-click on the server to update it to this release
            Install-ErfWebService.cmd   first install on a new server only
            Install-ErfWebMaintenance.cmd  nightly database backup and health monitor (once per server)
            README.txt                  step-by-step setup of the backup and health monitor
            appsettings.Example.json    template for the server's appsettings.json
            HOW-TO-DEPLOY.txt           the steps, for whoever is at the server
            *.ps1                       the scripts the .cmd files run

    Copy that one folder to the server and double-click Deploy-ErfWeb.cmd.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$backend = Split-Path -Parent $PSScriptRoot                  # ERFWeb\Backend
$erfweb = Split-Path -Parent $backend                         # ERFWeb
$project = Join-Path $backend 'src\ERFWeb.Api\ERFWeb.Api.csproj'

[xml] $props = Get-Content -Raw (Join-Path $backend 'Directory.Build.props')
$version = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if (-not $version) { throw 'No <Version> in Directory.Build.props.' }

$commit = (& git -C $erfweb rev-parse --short=7 HEAD).Trim()
$dirty = & git -C $erfweb status --porcelain -- Backend
if ($dirty) {
    Write-Warning 'Backend has uncommitted changes: they are included, but /api/health will show the last commit.'
}

$name = "ERFWeb-Api-$version-$commit"
$releases = Join-Path $erfweb 'releases'
$release = Join-Path $releases $name
$staging = Join-Path ([IO.Path]::GetTempPath()) "$name-publish"

foreach ($path in @($release, $staging)) {
    if (Test-Path $path) { Remove-Item -Recurse -Force $path }
}
New-Item -ItemType Directory -Force -Path $release | Out-Null

Write-Host "Publishing ERFWeb.Api $version ($commit)..."
& dotnet publish $project -c Release -o $staging --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

foreach ($leftover in @('appsettings.json', 'appsettings.Development.json')) {
    if (Test-Path (Join-Path $staging $leftover)) { throw "$leftover ended up in the publish output; it must stay on the server only." }
}

Compress-Archive -Path (Join-Path $staging '*') -DestinationPath (Join-Path $release 'ERFWeb-Api.zip')
# Everything the server needs travels with the release, so nothing is copied from the repo by hand.
foreach ($tool in @('Deploy-ErfWeb.cmd', 'Deploy-ErfWeb.ps1', 'Install-ErfWebService.cmd', 'Install-ErfWebService.ps1',
        'Install-ErfWebMaintenance.cmd', 'Install-ErfWebMaintenance.ps1', 'Backup-ErfWebDatabase.ps1', 'Test-ErfWebHealth.ps1',
        'ErfWebMaintenance.Common.ps1', 'README.txt', 'HOW-TO-DEPLOY.txt')) {
    Copy-Item (Join-Path $PSScriptRoot $tool) $release
}
Copy-Item (Join-Path $staging 'appsettings.Example.json') $release
Set-Content -Path (Join-Path $release 'release.txt') -Encoding ASCII -Value "$version+$commit"
Remove-Item -Recurse -Force $staging

$size = (Get-Item (Join-Path $release 'ERFWeb-Api.zip')).Length / 1MB
Write-Host ''
Write-Host ("Release ready ({0:N1} MB):" -f $size)
Write-Host "  $release"
Write-Host 'Copy this folder to the server and double-click Deploy-ErfWeb.cmd inside it.'
