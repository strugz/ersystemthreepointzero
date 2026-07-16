[CmdletBinding()]
param(
    [string]$WorkspaceRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$taskDisplayName = 'Daily Web Clean Architecture Audit'
$webRoot = Join-Path $WorkspaceRoot 'Web'
$backendRoot = Join-Path $webRoot 'Backend'
$frontendRoot = Join-Path $webRoot 'Frontend\ersystem-web-client'
$reportRoot = Join-Path $env:LOCALAPPDATA 'ERSystem\CleanArchitectureAudits'
$statePath = Join-Path $reportRoot 'state.json'
$runStartedUtc = [DateTime]::UtcNow

if (-not (Test-Path -LiteralPath $webRoot)) {
    throw "The Web workspace was not found at '$webRoot'."
}

[System.IO.Directory]::CreateDirectory($reportRoot) | Out-Null

$findings = New-Object 'System.Collections.Generic.List[object]'
$checks = New-Object 'System.Collections.Generic.List[object]'
$limitations = New-Object 'System.Collections.Generic.List[string]'
$changedFiles = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

function Add-Finding {
    param(
        [ValidateSet('FAIL', 'WARNING')]
        [string]$Severity,
        [string]$Rule,
        [string]$File,
        [string]$DependencyPath,
        [string]$Recommendation
    )

    $findings.Add([pscustomobject]@{
        Severity = $Severity
        Rule = $Rule
        File = $File
        DependencyPath = $DependencyPath
        Recommendation = $Recommendation
    })
}

function Add-Check {
    param(
        [string]$Name,
        [ValidateSet('PASS', 'FAIL', 'LIMITATION')]
        [string]$Status,
        [string]$Details
    )

    $checks.Add([pscustomobject]@{
        Name = $Name
        Status = $Status
        Details = $Details
    })
}

function Get-RelativeWorkspacePath {
    param([string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $workspace = [System.IO.Path]::GetFullPath($WorkspaceRoot).TrimEnd('\') + '\'
    if ($resolved.StartsWith($workspace, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolved.Substring($workspace.Length).Replace('\', '/')
    }

    return $resolved.Replace('\', '/')
}

function Invoke-AuditCommand {
    param(
        [string]$Name,
        [string]$Executable,
        [string[]]$Arguments,
        [string]$WorkingDirectory
    )

    $previousLocation = Get-Location
    try {
        Set-Location -LiteralPath $WorkingDirectory
        $output = (& $Executable @Arguments 2>&1 | Out-String).Trim()
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 0) {
            Add-Check -Name $Name -Status 'PASS' -Details 'Completed successfully.'
        }
        else {
            if ($output.Length -gt 5000) {
                $output = $output.Substring($output.Length - 5000)
            }
            Add-Check -Name $Name -Status 'FAIL' -Details $output
        }
    }
    catch {
        Add-Check -Name $Name -Status 'FAIL' -Details $_.Exception.Message
    }
    finally {
        Set-Location -LiteralPath $previousLocation
    }
}

function Get-ProjectReferenceNames {
    param([string]$ProjectPath)

    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $references = @()
    foreach ($reference in @($project.SelectNodes('//ProjectReference'))) {
        if ($null -ne $reference -and -not [string]::IsNullOrWhiteSpace([string]$reference.Include)) {
            $references += [System.IO.Path]::GetFileNameWithoutExtension([string]$reference.Include)
        }
    }
    return @($references | Sort-Object -Unique)
}

function Test-ProjectReferences {
    param(
        [string]$ProjectPath,
        [string[]]$ExpectedReferences
    )

    $actual = @(Get-ProjectReferenceNames -ProjectPath $ProjectPath)
    $expected = @($ExpectedReferences | Sort-Object -Unique)
    if (($expected -join '|') -ne ($actual -join '|')) {
        Add-Finding `
            -Severity 'FAIL' `
            -Rule 'Backend project dependency direction' `
            -File (Get-RelativeWorkspacePath $ProjectPath) `
            -DependencyPath ("Expected [{0}], found [{1}]" -f ($expected -join ', '), ($actual -join ', ')) `
            -Recommendation 'Restore the documented Api/Application/Domain/Infrastructure dependency direction.'
    }
}

function Get-SourceFiles {
    param(
        [string]$Root,
        [string[]]$Extensions
    )

    return @(Get-ChildItem -LiteralPath $Root -Recurse -File | Where-Object {
        $Extensions -contains $_.Extension.ToLowerInvariant() -and
        $_.FullName -notmatch '\\(bin|obj|dist|node_modules|coverage)\\'
    })
}

$gitCommand = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $gitCommand) {
    $limitations.Add('Git is unavailable; the change window could not be calculated.')
}
else {
    $lastRunUtc = $runStartedUtc.AddDays(-1)
    if (Test-Path -LiteralPath $statePath) {
        try {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            if ($state.LastRunUtc) {
                $lastRunUtc = [DateTime]::Parse(
                    [string]$state.LastRunUtc,
                    [System.Globalization.CultureInfo]::InvariantCulture,
                    [System.Globalization.DateTimeStyles]::RoundtripKind)
            }
        }
        catch {
            $limitations.Add('The previous audit state could not be read; a 24-hour change window was used.')
        }
    }

    $statusLines = @(& $gitCommand.Source -C $WorkspaceRoot status --porcelain -- Web 2>$null)
    foreach ($line in $statusLines) {
        if ($line.Length -gt 3) {
            $path = $line.Substring(3).Trim()
            if ($path.Contains(' -> ')) {
                $path = $path.Split(@(' -> '), [System.StringSplitOptions]::None)[-1]
            }
            $changedFiles.Add($path.Replace('\', '/').Trim('"')) | Out-Null
        }
    }

    $sinceArgument = '--since=' + $lastRunUtc.ToUniversalTime().ToString('o')
    $commitFiles = @(& $gitCommand.Source -C $WorkspaceRoot log $sinceArgument --name-only --pretty=format: -- Web 2>$null)
    foreach ($path in $commitFiles) {
        if (-not [string]::IsNullOrWhiteSpace($path)) {
            $changedFiles.Add($path.Trim().Replace('\', '/')) | Out-Null
        }
    }
}

$apiProject = Join-Path $backendRoot 'src\ERSystem.Web.Api\ERSystem.Web.Api.csproj'
$applicationProject = Join-Path $backendRoot 'src\ERSystem.Web.Application\ERSystem.Web.Application.csproj'
$domainProject = Join-Path $backendRoot 'src\ERSystem.Web.Domain\ERSystem.Web.Domain.csproj'
$infrastructureProject = Join-Path $backendRoot 'src\ERSystem.Web.Infrastructure\ERSystem.Web.Infrastructure.csproj'

Test-ProjectReferences -ProjectPath $domainProject -ExpectedReferences @()
Test-ProjectReferences -ProjectPath $applicationProject -ExpectedReferences @('ERSystem.Web.Domain')
Test-ProjectReferences -ProjectPath $infrastructureProject -ExpectedReferences @(
    'ERSystem.Web.Application',
    'ERSystem.Web.Domain'
)
Test-ProjectReferences -ProjectPath $apiProject -ExpectedReferences @(
    'ERSystem.Web.Application',
    'ERSystem.Web.Infrastructure'
)

$domainSource = Join-Path $backendRoot 'src\ERSystem.Web.Domain'
foreach ($file in Get-SourceFiles -Root $domainSource -Extensions @('.cs')) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match 'ERSystem\.Web\.(Application|Infrastructure|Api)|Microsoft\.(EntityFrameworkCore|AspNetCore|Extensions\.Configuration)|System\.Data\.SqlClient') {
        Add-Finding `
            -Severity 'FAIL' `
            -Rule 'Domain must remain framework and outer-layer independent' `
            -File (Get-RelativeWorkspacePath $file.FullName) `
            -DependencyPath 'Domain -> Application/Infrastructure/API/framework' `
            -Recommendation 'Move the dependency to an outer layer and keep only pure workflow rules in Domain.'
    }
}

$applicationSource = Join-Path $backendRoot 'src\ERSystem.Web.Application'
foreach ($file in Get-SourceFiles -Root $applicationSource -Extensions @('.cs')) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match 'ERSystem\.Web\.(Infrastructure|Api)|Microsoft\.(EntityFrameworkCore|AspNetCore)') {
        Add-Finding `
            -Severity 'FAIL' `
            -Rule 'Application cannot depend on Infrastructure, API, EF Core, or ASP.NET Core' `
            -File (Get-RelativeWorkspacePath $file.FullName) `
            -DependencyPath 'Application -> outer layer/framework' `
            -Recommendation 'Keep contracts and orchestration in Application; implement framework behavior in Infrastructure or API.'
    }
}

$controllerRoot = Join-Path $backendRoot 'src\ERSystem.Web.Api\Controllers'
foreach ($file in Get-SourceFiles -Root $controllerRoot -Extensions @('.cs')) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match '\b(DbContext|DbSet|SqlCommand|FromSql|SaveChanges|ExecuteSql|Database\.)\b') {
        Add-Finding `
            -Severity 'FAIL' `
            -Rule 'Controllers must remain thin and persistence-free' `
            -File (Get-RelativeWorkspacePath $file.FullName) `
            -DependencyPath 'API controller -> database implementation' `
            -Recommendation 'Move persistence and workflow behavior into an Application contract implemented by Infrastructure.'
    }
}

$sharedRoot = Join-Path $frontendRoot 'src\shared'
$featureRoot = Join-Path $frontendRoot 'src\features'
$vueSourceRoot = Join-Path $frontendRoot 'src'
$importPattern = '(?:from\s+|import\s*\()[''"](?<path>@/[^''"]+)[''"]'

foreach ($file in Get-SourceFiles -Root $sharedRoot -Extensions @('.ts', '.vue')) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($content, $importPattern)) {
        $importPath = $match.Groups['path'].Value
        if ($importPath -match '^@/(app|views|layouts|features)/') {
            Add-Finding `
                -Severity 'FAIL' `
                -Rule 'Shared frontend code cannot depend on outer frontend layers' `
                -File (Get-RelativeWorkspacePath $file.FullName) `
                -DependencyPath ("shared -> {0}" -f $importPath) `
                -Recommendation 'Move the coordination into a view/layout/app layer or pass the dependency through typed props/composable parameters.'
        }
    }
}

foreach ($file in Get-SourceFiles -Root $featureRoot -Extensions @('.ts', '.vue')) {
    $relativeFeaturePath = $file.FullName.Substring($featureRoot.Length).TrimStart('\')
    $currentFeature = $relativeFeaturePath.Split('\')[0]
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($content, $importPattern)) {
        $importPath = $match.Groups['path'].Value
        if ($importPath -match '^@/features/(?<feature>[^/]+)') {
            $targetFeature = $Matches['feature']
            if ($targetFeature -ne $currentFeature) {
                Add-Finding `
                    -Severity 'FAIL' `
                    -Rule 'Frontend features cannot import other features directly' `
                    -File (Get-RelativeWorkspacePath $file.FullName) `
                    -DependencyPath ("features/{0} -> features/{1}" -f $currentFeature, $targetFeature) `
                    -Recommendation 'Coordinate the two features in a route-level view or move genuinely reusable code into shared.'
            }
        }
    }
}

foreach ($file in Get-SourceFiles -Root $vueSourceRoot -Extensions @('.vue')) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match '\b(fetch|apiRequest|apiBlob)\s*(?:<[^>]+>)?\s*\(') {
        Add-Finding `
            -Severity 'FAIL' `
            -Rule 'Vue components cannot perform direct HTTP requests' `
            -File (Get-RelativeWorkspacePath $file.FullName) `
            -DependencyPath 'Vue component -> HTTP client' `
            -Recommendation 'Move the call into a typed feature API module or the shared API client.'
    }
    if ($content -match '(?is)\bSELECT\b.{0,200}\bFROM\b|\bINSERT\s+INTO\b|\bDELETE\s+FROM\b|\bUPDATE\s+\w+\s+SET\b') {
        Add-Finding `
            -Severity 'FAIL' `
            -Rule 'Vue components cannot contain business SQL' `
            -File (Get-RelativeWorkspacePath $file.FullName) `
            -DependencyPath 'Vue component -> SQL' `
            -Recommendation 'Move all SQL and persistence behavior to the backend Infrastructure layer.'
    }
}

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnetCommand) {
    $limitations.Add('The .NET SDK is unavailable; backend architecture tests were not run.')
    Add-Check -Name 'Backend architecture tests' -Status 'LIMITATION' -Details 'dotnet was not found.'
}
else {
    Invoke-AuditCommand `
        -Name 'Backend architecture tests' `
        -Executable $dotnetCommand.Source `
        -Arguments @(
            'test',
            'ERSystem.Web.sln',
            '-c',
            'Release',
            '--no-restore',
            '--filter',
            'FullyQualifiedName~ERSystem.Web.Tests.Architecture'
        ) `
        -WorkingDirectory $backendRoot
}

$npmCommand = Get-Command npm.cmd -ErrorAction SilentlyContinue
if ($null -eq $npmCommand) {
    $limitations.Add('Node/npm is unavailable; frontend lint and type-check were not run.')
    Add-Check -Name 'Frontend lint' -Status 'LIMITATION' -Details 'npm.cmd was not found.'
    Add-Check -Name 'Frontend type-check' -Status 'LIMITATION' -Details 'npm.cmd was not found.'
}
else {
    Invoke-AuditCommand `
        -Name 'Frontend lint' `
        -Executable $npmCommand.Source `
        -Arguments @('run', 'lint') `
        -WorkingDirectory $frontendRoot
    Invoke-AuditCommand `
        -Name 'Frontend type-check' `
        -Executable $npmCommand.Source `
        -Arguments @('run', 'type-check') `
        -WorkingDirectory $frontendRoot
}

$hasFailedCheck = @($checks | Where-Object { $_.Status -eq 'FAIL' }).Count -gt 0
$hasFailedFinding = @($findings | Where-Object { $_.Severity -eq 'FAIL' }).Count -gt 0
$hasWarning = @($findings | Where-Object { $_.Severity -eq 'WARNING' }).Count -gt 0

$overall = 'PASS'
if ($hasFailedCheck -or $hasFailedFinding) {
    $overall = 'FAIL'
}
elseif ($hasWarning -or $limitations.Count -gt 0) {
    $overall = 'WARNING'
}

$reportLines = New-Object 'System.Collections.Generic.List[string]'
$reportLines.Add('# Daily Web Clean Architecture Audit')
$reportLines.Add('')
$reportLines.Add(('- Overall: **{0}**' -f $overall))
$reportLines.Add(('- Started: `{0}`' -f $runStartedUtc.ToString('o')))
$reportLines.Add(('- Workspace: `{0}`' -f $WorkspaceRoot))
$reportLines.Add(('- Scope: `Web/` only'))
$reportLines.Add('')
$reportLines.Add('## Change window')
$reportLines.Add('')
if ($changedFiles.Count -eq 0) {
    $reportLines.Add('No committed or uncommitted Web changes were found in the audit window.')
}
else {
    foreach ($path in @($changedFiles | Sort-Object)) {
        $reportLines.Add(('- `{0}`' -f $path))
    }
}

$reportLines.Add('')
$reportLines.Add('## Invariant checks')
$reportLines.Add('')
$reportLines.Add('| Check | Result | Details |')
$reportLines.Add('| --- | --- | --- |')
foreach ($check in $checks) {
    $details = ([string]$check.Details).Replace('|', '\|').Replace("`r", ' ').Replace("`n", '<br>')
    $reportLines.Add(('| {0} | **{1}** | {2} |' -f $check.Name, $check.Status, $details))
}

$reportLines.Add('')
$reportLines.Add('## Findings')
$reportLines.Add('')
if ($findings.Count -eq 0) {
    $reportLines.Add('No Clean Architecture violations found.')
}
else {
    $reportLines.Add('| Severity | File | Rule | Dependency path | Recommended correction |')
    $reportLines.Add('| --- | --- | --- | --- | --- |')
    foreach ($finding in $findings) {
        $fileText = ([string]$finding.File).Replace('|', '\|')
        $ruleText = ([string]$finding.Rule).Replace('|', '\|')
        $pathText = ([string]$finding.DependencyPath).Replace('|', '\|')
        $fixText = ([string]$finding.Recommendation).Replace('|', '\|')
        $reportLines.Add(('| **{0}** | `{1}` | {2} | `{3}` | {4} |' -f
            $finding.Severity, $fileText, $ruleText, $pathText, $fixText))
    }
}

$reportLines.Add('')
$reportLines.Add('## Limitations')
$reportLines.Add('')
if ($limitations.Count -eq 0) {
    $reportLines.Add('None.')
}
else {
    foreach ($limitation in $limitations) {
        $reportLines.Add(('- {0}' -f $limitation))
    }
}

$reportName = 'audit-{0}.md' -f (Get-Date).ToString('yyyyMMdd-HHmmss')
$reportPath = Join-Path $reportRoot $reportName
$latestPath = Join-Path $reportRoot 'latest.md'
$reportText = $reportLines -join [Environment]::NewLine
[System.IO.File]::WriteAllText($reportPath, $reportText, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText($latestPath, $reportText, [System.Text.UTF8Encoding]::new($false))

$head = ''
if ($null -ne $gitCommand) {
    $head = (& $gitCommand.Source -C $WorkspaceRoot rev-parse HEAD 2>$null | Out-String).Trim()
}
$state = [pscustomobject]@{
    LastRunUtc = $runStartedUtc.ToString('o')
    LastHead = $head
    LastOverall = $overall
    LastReport = $reportPath
}
[System.IO.File]::WriteAllText(
    $statePath,
    ($state | ConvertTo-Json),
    [System.Text.UTF8Encoding]::new($false))

Write-Output ('Overall: {0}' -f $overall)
Write-Output ('Report: {0}' -f $reportPath)

if ($overall -eq 'FAIL') {
    exit 2
}
if ($overall -eq 'WARNING') {
    exit 1
}
exit 0
