# =============================================================================
# DataShare - Restauration de la base PostgreSQL (Windows / PowerShell)
# Usage : .\scripts\db-restore.ps1 -BackupFile .\backups\datashare_backup_2026-08-02.sql
# =============================================================================
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $BackupFile)) { Write-Error "Fichier introuvable : $BackupFile" }

Write-Host "==> Restauration de la base 'datashare' depuis $BackupFile..."
Get-Content $BackupFile | docker exec -i oc-p4-datashare-postgres psql -U datashare datashare
if ($LASTEXITCODE -ne 0) { Write-Error "Echec de la restauration." }

Write-Host "Restauration terminee." -ForegroundColor Green
