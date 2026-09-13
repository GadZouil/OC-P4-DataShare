# =============================================================================
# DataShare - Verification qualite avant livraison / soutenance (Windows)
#
#   .\scripts\verify.ps1            # tests + couverture + build front + audits
#   .\scripts\verify.ps1 -Quick     # sans couverture ni audits (juste tests + build)
#   Les tests E2E Cypress et k6 ne sont pas lances ici (stack Docker requise).
#
# Resultats : backend\TestResults\CoverageReport\index.html (rapport HTML)
#             backend\TestResults\CoverageReport\Summary.txt (resume texte)
# =============================================================================
param(
    [switch]$Quick
)

$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$results = [ordered]@{}
$sw = [System.Diagnostics.Stopwatch]::StartNew()

function Step($name, [scriptblock]$action) {
    Write-Host ""
    Write-Host "==> $name" -ForegroundColor Cyan
    $ok = $false
    try {
        & $action
        $ok = ($LASTEXITCODE -eq 0 -or $null -eq $LASTEXITCODE)
    } catch {
        Write-Host $_ -ForegroundColor Red
        $ok = $false
    }
    $script:results[$name] = $ok
    if (-not $ok) { Write-Host "    -> ECHEC : $name" -ForegroundColor Red }
}

# --- Prerequis ---------------------------------------------------------------
foreach ($tool in @("dotnet", "node", "npm")) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Write-Error "$tool est introuvable dans le PATH."
        exit 1
    }
}
Write-Host ("dotnet " + (dotnet --version) + " | node " + (node --version) + " | npm " + (npm --version))

# --- Backend : tests (+ couverture) ------------------------------------------
if ($Quick) {
    Step "Backend - dotnet test" {
        dotnet test backend/DataShare.sln --nologo
    }
} else {
    Step "Backend - dotnet test + couverture" {
        if (Test-Path backend/TestResults) { Remove-Item backend/TestResults -Recurse -Force }
        dotnet test backend/DataShare.sln --nologo `
            --collect:"XPlat Code Coverage" `
            --settings backend/DataShare.Api.Tests/coverlet.runsettings `
            --results-directory backend/TestResults
    }
    Step "Backend - rapport de couverture (reportgenerator)" {
        if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
            Write-Host "    Installation de dotnet-reportgenerator-globaltool..."
            dotnet tool install -g dotnet-reportgenerator-globaltool | Out-Null
        }
        reportgenerator -reports:"backend/TestResults/**/coverage.cobertura.xml" `
            -targetdir:"backend/TestResults/CoverageReport" -reporttypes:"Html;TextSummary" | Out-Null
        if (Test-Path backend/TestResults/CoverageReport/Summary.txt) {
            Get-Content backend/TestResults/CoverageReport/Summary.txt | Select-String "Line coverage|Branch coverage|Covered lines|Total lines" | ForEach-Object { Write-Host "    $_" }
        }
    }
}

# --- Frontend : build + lint --------------------------------------------------
Step "Frontend - npm ci" {
    Push-Location frontend/datashare-front
    npm ci --no-audit --no-fund
    Pop-Location
}
Step "Frontend - npm run build (type-check + vite)" {
    Push-Location frontend/datashare-front
    npm run build
    Pop-Location
}
Step "Frontend - npm run lint" {
    Push-Location frontend/datashare-front
    npm run lint
    Pop-Location
}

# --- Audits de securite -------------------------------------------------------
if (-not $Quick) {
    Step "Audit - dotnet list package --vulnerable" {
        dotnet list backend/DataShare.sln package --vulnerable --include-transitive
    }
    Step "Audit - npm audit (frontend/datashare-front)" {
        Push-Location frontend/datashare-front
        npm audit
        Pop-Location
    }
    Step "Audit - npm audit (frontend, Cypress)" {
        Push-Location frontend
        $env:CYPRESS_INSTALL_BINARY = "0"
        npm ci --no-audit --no-fund | Out-Null
        npm audit
        Remove-Item Env:CYPRESS_INSTALL_BINARY
        Pop-Location
    }
}

# --- Resume ------------------------------------------------------------------
$sw.Stop()
Write-Host ""
Write-Host "================= RESUME ($([int]$sw.Elapsed.TotalSeconds) s) =================" -ForegroundColor Cyan
$failed = 0
foreach ($k in $results.Keys) {
    if ($results[$k]) { Write-Host ("  [OK]    " + $k) -ForegroundColor Green }
    else { Write-Host ("  [ECHEC] " + $k) -ForegroundColor Red; $failed++ }
}
if (-not $Quick -and (Test-Path backend/TestResults/CoverageReport/index.html)) {
    Write-Host ""
    Write-Host "  Rapport de couverture : backend\TestResults\CoverageReport\index.html"
    Write-Host "  (a ouvrir dans le navigateur, puis capture -> docs\coverage-report.png)"
}
Write-Host ""
Write-Host "  Tests E2E (stack Docker lancee) : cd frontend ; npx cypress run"
Write-Host "  Charge k6 (API Docker)          : k6 run perf/k6-upload-test.js"
Write-Host ""
if ($failed -gt 0) { exit 1 } else { exit 0 }
