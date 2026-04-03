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
$coverageSummaryPath = Join-Path $resultsRoot "coverage-summary.json"
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
    npm run test:coverage

    if (-not (Test-Path -LiteralPath $coverageSummaryPath)) {
        Fail-Check "Client coverage summary was not generated at '$coverageSummaryPath'."
    }

    $coverageSummary = Get-Content -LiteralPath $coverageSummaryPath -Raw | ConvertFrom-Json
    $lineCoverage = [double]$coverageSummary.total.lines.pct

    if ($env:GITHUB_STEP_SUMMARY) {
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "## Client Coverage Result"
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value ""
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "Client line coverage: $lineCoverage%"
    }

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
