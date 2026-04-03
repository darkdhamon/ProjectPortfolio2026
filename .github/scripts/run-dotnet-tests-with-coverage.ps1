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
$resultsRoot = Join-Path $solutionRoot "CoverageResults\dotnet"
$coverageSettingsPath = Join-Path $solutionRoot "coverage.runsettings"
$minimumCoverage = if ($env:MINIMUM_COVERAGE) { [double]$env:MINIMUM_COVERAGE } else { 70.0 }
$enforceCoverageGate = $true

if ($env:ENFORCE_COVERAGE_GATE) {
    $enforceCoverageGate = $env:ENFORCE_COVERAGE_GATE -eq "true"
}

try {
    if (Test-Path -LiteralPath $resultsRoot) {
        Remove-Item -LiteralPath $resultsRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $resultsRoot | Out-Null

    if (-not (Test-Path -LiteralPath $coverageSettingsPath)) {
        Fail-Check "Coverage settings file was not found at '$coverageSettingsPath'."
    }

    $testProjects = Get-ChildItem -Path $solutionRoot -Recurse -Filter *.csproj |
        Where-Object { $_.Name -match 'Tests?\.csproj$' -or $_.BaseName -match 'Tests?$' }

    if (-not $testProjects -and $enforceCoverageGate) {
        Fail-Check "No .NET test projects were found. Add unit tests to satisfy repository policy."
    }

    if (-not $testProjects) {
        Write-Host "::warning::No .NET test projects were found. Coverage is being reported as advisory because coverage enforcement is disabled for this branch."
        if ($env:GITHUB_STEP_SUMMARY) {
            Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "## Coverage Check Warning"
            Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
            Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "No .NET test projects were found. Coverage enforcement is disabled for this branch."
        }
        return
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
            --logger "trx" `
            --results-directory $resultsRoot `
            --settings $coverageSettingsPath `
            --collect:"XPlat Code Coverage"
    }

    $coverageFiles = Get-ChildItem -Path $resultsRoot -Recurse -Filter coverage.cobertura.xml
    $testResultFiles = Get-ChildItem -Path $resultsRoot -Recurse -Filter *.trx

    if (-not $coverageFiles) {
        Fail-Check "Coverage output was not generated. Ensure the test projects include a supported coverage collector."
    }

    if (-not $testResultFiles) {
        Fail-Check "Test result output was not generated. Ensure dotnet test completed successfully."
    }

    [double]$coveredLines = 0
    [double]$validLines = 0
    [int]$passedTests = 0
    [int]$failedTests = 0
    [int]$skippedTests = 0
    [int]$totalTests = 0

    foreach ($file in $coverageFiles) {
        [xml]$coverageXml = Get-Content -LiteralPath $file.FullName
        $coveredLines += [double]$coverageXml.coverage.'lines-covered'
        $validLines += [double]$coverageXml.coverage.'lines-valid'
    }

    foreach ($file in $testResultFiles) {
        [xml]$testResultsXml = Get-Content -LiteralPath $file.FullName
        $resultSummary = $testResultsXml.TestRun.ResultSummary.Counters

        $passedTests += [int]$resultSummary.passed
        $failedTests += [int]$resultSummary.failed
        $skippedTests += [int]$resultSummary.notExecuted
        $totalTests += [int]$resultSummary.total
    }

    if ($validLines -le 0) {
        Fail-Check "Coverage report did not contain any measurable lines."
    }

    $coveragePercent = [math]::Round(($coveredLines / $validLines) * 100, 2)
    Write-Host "Combined .NET line coverage: $coveragePercent%"
    Write-Host ".NET test files: $($testResultFiles.Count)"
    Write-Host ".NET test results: $passedTests passed, $failedTests failed, $skippedTests skipped, $totalTests total"

    if ($env:GITHUB_STEP_SUMMARY) {
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "## .NET Test Report"
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "Test Files: $($testResultFiles.Count)"
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "Test Results: $passedTests passed, $failedTests failed, $skippedTests skipped, $totalTests total"
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "## Coverage Check Result"
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "Combined .NET line coverage: $coveragePercent%"
    }

    if ($coveragePercent -lt $minimumCoverage -and $enforceCoverageGate) {
        Fail-Check "Coverage check failed. Required: $minimumCoverage%. Actual: $coveragePercent%."
    }

    if ($coveragePercent -lt $minimumCoverage) {
        Write-Host "::warning::Coverage is below the $minimumCoverage% target, but coverage enforcement is disabled for this branch. Actual: $coveragePercent%."
        if ($env:GITHUB_STEP_SUMMARY) {
            Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
            Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "Coverage is below the target, but enforcement is disabled for this branch."
        }
    }
}
catch {
    if (-not $_.Exception.Message) {
        throw
    }

    throw $_
}
