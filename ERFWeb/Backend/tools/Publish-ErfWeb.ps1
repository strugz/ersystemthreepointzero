<#
.SYNOPSIS
    Builds the ERFWeb API and packages it as one release folder to copy to the server.

.DESCRIPTION
    Run on the developer PC (double-click Publish-ErfWeb.cmd). It publishes ERFWeb.Api (Release) into a clean
    folder, so no stale file from an older build is included, and writes:

        ERFWeb\releases\ERFWeb-Api-<version>-<commit>\
            ERFWeb-Api.zip          the whole API (never appsettings.json; the server keeps its own)
            Deploy-ErfWeb.cmd       double-click on the server to install this release
            Deploy-ErfWeb.ps1

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
Copy-Item (Join-Path $PSScriptRoot 'Deploy-ErfWeb.cmd') $release
Copy-Item (Join-Path $PSScriptRoot 'Deploy-ErfWeb.ps1') $release
Set-Content -Path (Join-Path $release 'release.txt') -Encoding ASCII -Value "$version+$commit"
Remove-Item -Recurse -Force $staging

$size = (Get-Item (Join-Path $release 'ERFWeb-Api.zip')).Length / 1MB
Write-Host ''
Write-Host ("Release ready ({0:N1} MB):" -f $size)
Write-Host "  $release"
Write-Host 'Copy this folder to the server and double-click Deploy-ErfWeb.cmd inside it.'
