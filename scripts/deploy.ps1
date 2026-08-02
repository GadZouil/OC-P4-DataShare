# =============================================================================
# DataShare - Script de deploiement complet (Windows / PowerShell)
# Installe et lance l'application : PostgreSQL + API + Frontend via Docker.
# Les migrations EF Core sont appliquees automatiquement au demarrage de l'API.
#
# Usage :   .\scripts\deploy.ps1           # deploiement standard
#           .\scripts\deploy.ps1 -Reset    # reinitialise la base de donnees
# =============================================================================
param(
    [switch]$Reset
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "==> Verification des prerequis..." -ForegroundColor Cyan
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker n'est pas installe ou pas dans le PATH. https://docs.docker.com/get-docker/"
}
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Le demon Docker ne repond pas. Lancez Docker Desktop puis reessayez."
}

if ($Reset) {
    Write-Host "==> Reinitialisation : suppression des conteneurs et du volume BDD..." -ForegroundColor Yellow
    docker compose down -v
}

Write-Host "==> Build et lancement de la stack (db + api + frontend)..." -ForegroundColor Cyan
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { Write-Error "Echec du docker compose up." }

Write-Host "==> Attente de la disponibilite de l'API..." -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds(90)
$ok = $false
while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-RestMethod -Uri "http://localhost:5000/api/health" -TimeoutSec 3
        if ($r.status -eq "ok") { $ok = $true; break }
    } catch { Start-Sleep -Seconds 3 }
}
if (-not $ok) {
    Write-Error "L'API ne repond pas apres 90s. Consultez les logs : docker compose logs api"
}

Write-Host ""
Write-Host "Deploiement termine avec succes." -ForegroundColor Green
Write-Host "  Frontend  : http://localhost"
Write-Host "  API       : http://localhost:5000  (sante : /api/health)"
Write-Host "  PostgreSQL: localhost:5433 (base 'datashare')"
Write-Host ""
Write-Host "Arret : docker compose down    (ajouter -v pour effacer les donnees)"
