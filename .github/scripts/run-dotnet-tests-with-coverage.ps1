$ErrorActionPreference = "Stop"

function Fail-Check {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host "::error::$Message"

    if ($env:GITHUB_STEP_SUMMARY) {
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "## Coverage Check Failure"
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $Message
    }

    throw $Message
}

$solutionRoot = Join-Path $PSScriptRoot "..\..\ProjectPortfolio2026"
$resultsRoot = Join-Path $solutionRoot "TestResults"
$minimumCoverage = 70.0

try {
    if (Test-Path -LiteralPath $resultsRoot) {
        Remove-Item -LiteralPath $resultsRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $resultsRoot | Out-Null

    $testProjects = Get-ChildItem -Path $solutionRoot -Recurse -Filter *.csproj |
        Where-Object { $_.Name -match 'Tests?\.csproj$' -or $_.BaseName -match 'Tests?$' }

    if (-not $testProjects) {
        Fail-Check "No .NET test projects were found. Add unit tests to satisfy repository policy."
    }

    foreach ($project in $testProjects) {
        $projectXml = Get-Content -LiteralPath $project.FullName -Raw
        $hasCoverageCollector =
            $projectXml -match 'coverlet\.collector' -or
            $projectXml -match 'coverlet\.msbuild' -or
            $projectXml -match 'Microsoft\.Testing\.Extensions\.CodeCoverage'

        if (-not $hasCoverageCollector) {
            Fail-Check "Test project '$($project.Name)' does not declare a supported coverage collector package."
        }

        dotnet test $project.FullName `
            --configuration Release `
            --no-build `
            --logger "trx" `
            --results-directory $resultsRoot `
            --collect:"XPlat Code Coverage"
    }

    $coverageFiles = Get-ChildItem -Path $resultsRoot -Recurse -Filter coverage.cobertura.xml

    if (-not $coverageFiles) {
        Fail-Check "Coverage output was not generated. Ensure the test projects include a supported coverage collector."
    }

    [double]$coveredLines = 0
    [double]$validLines = 0

    foreach ($file in $coverageFiles) {
        [xml]$coverageXml = Get-Content -LiteralPath $file.FullName
        $coveredLines += [double]$coverageXml.coverage.'lines-covered'
        $validLines += [double]$coverageXml.coverage.'lines-valid'
    }

    if ($validLines -le 0) {
        Fail-Check "Coverage report did not contain any measurable lines."
    }

    $coveragePercent = [math]::Round(($coveredLines / $validLines) * 100, 2)
    Write-Host "Combined .NET line coverage: $coveragePercent%"

    if ($env:GITHUB_STEP_SUMMARY) {
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "## Coverage Check Result"
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "Combined .NET line coverage: $coveragePercent%"
    }

    if ($coveragePercent -lt $minimumCoverage) {
        Fail-Check "Coverage check failed. Required: $minimumCoverage%. Actual: $coveragePercent%."
    }
}
catch {
    if (-not $_.Exception.Message) {
        throw
    }

    throw $_
}
