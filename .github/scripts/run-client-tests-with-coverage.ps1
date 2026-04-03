$ErrorActionPreference = "Stop"

function Fail-Check {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host "::error::$Message"
    throw $Message
}

$solutionRoot = Join-Path $PSScriptRoot "..\..\ProjectPortfolio2026"
$clientRoot = Join-Path $solutionRoot "projectportfolio2026.client"
$resultsRoot = Join-Path $solutionRoot "CoverageResults\client"
$summaryRoot = Join-Path $solutionRoot "CoverageResults\summary"
$coverageSummaryPath = Join-Path $resultsRoot "coverage-summary.json"
$testResultsPath = Join-Path $resultsRoot "vitest-results.json"
$minimumCoverage = if ($env:MINIMUM_COVERAGE) { [double]$env:MINIMUM_COVERAGE } else { 70.0 }
$enforceCoverageGate = $true

if ($env:ENFORCE_COVERAGE_GATE) {
    $enforceCoverageGate = $env:ENFORCE_COVERAGE_GATE -eq "true"
}

if (-not (Test-Path -LiteralPath $clientRoot)) {
    Fail-Check "Client root was not found at '$clientRoot'."
}

Push-Location $clientRoot

try {
    npx vitest run --coverage --pool=threads --maxWorkers=1 --no-file-parallelism --reporter=default --reporter=json --outputFile=$testResultsPath

    if (-not (Test-Path -LiteralPath $coverageSummaryPath)) {
        Fail-Check "Client coverage summary was not generated at '$coverageSummaryPath'."
    }

    if (-not (Test-Path -LiteralPath $testResultsPath)) {
        Fail-Check "Client test result output was not generated at '$testResultsPath'."
    }

    $coverageSummary = Get-Content -LiteralPath $coverageSummaryPath -Raw | ConvertFrom-Json
    $testSummary = Get-Content -LiteralPath $testResultsPath -Raw | ConvertFrom-Json
    $lineCoverage = [double]$coverageSummary.total.lines.pct
    $clientTestFiles = @($testSummary.testResults).Count
    $clientPassedTests = [int]$testSummary.numPassedTests
    $clientFailedTests = [int]$testSummary.numFailedTests
    $clientSkippedTests = [int]$testSummary.numPendingTests
    $clientTotalTests = [int]$testSummary.numTotalTests

    if ($env:GITHUB_STEP_SUMMARY) {
        $summaryRows = @()
        $dotnetSummaryPath = Join-Path $summaryRoot 'dotnet-summary.json'

        if (Test-Path -LiteralPath $dotnetSummaryPath) {
            $dotnetSummary = Get-Content -LiteralPath $dotnetSummaryPath -Raw | ConvertFrom-Json
            $summaryRows += "| $($dotnetSummary.suite) | $($dotnetSummary.testFiles) | $($dotnetSummary.passedTests) passed, $($dotnetSummary.failedTests) failed, $($dotnetSummary.skippedTests) skipped, $($dotnetSummary.totalTests) total | $($dotnetSummary.lineCoverage)% |"
        }

        $summaryRows += "| Client | $clientTestFiles | $clientPassedTests passed, $clientFailedTests failed, $clientSkippedTests skipped, $clientTotalTests total | $lineCoverage% |"

        $summaryLines = @(
            '## Test And Coverage Summary',
            '',
            '| Suite | Test Files | Test Results | Line Coverage |',
            '| --- | ---: | --- | ---: |'
        ) + $summaryRows

        Set-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summaryLines
    }

    Write-Host "Client test files: $clientTestFiles"
    Write-Host "Client test results: $clientPassedTests passed, $clientFailedTests failed, $clientSkippedTests skipped, $clientTotalTests total"
    Write-Host "Client line coverage: $lineCoverage%"

    if ($lineCoverage -lt $minimumCoverage -and $enforceCoverageGate) {
        Fail-Check "Client coverage check failed. Required: $minimumCoverage%. Actual: $lineCoverage%."
    }

    if ($lineCoverage -lt $minimumCoverage) {
        Write-Host "::warning::Client coverage is below the $minimumCoverage% target, but coverage enforcement is disabled for this branch. Actual: $lineCoverage%."
    }
}
finally {
    Pop-Location
}
