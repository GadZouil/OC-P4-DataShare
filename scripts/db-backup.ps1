# =============================================================================
# DataShare - Sauvegarde de la base PostgreSQL (Windows / PowerShell)
# Usage : .\scripts\db-backup.ps1 [-OutDir .\backups]
# =============================================================================
param(
    [string]$OutDir = ".\backups"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$file = Join-Path $OutDir ("datashare_backup_{0}.sql" -f (Get-Date -Format "yyyy-MM-dd_HHmmss"))

Write-Host "==> Sauvegarde de la base 'datashare' vers $file..."
docker exec -t oc-p4-datashare-postgres pg_dump -U datashare datashare | Out-File -Encoding utf8 $file
if ($LASTEXITCODE -ne 0) { Write-Error "Echec du pg_dump. Le conteneur 'oc-p4-datashare-postgres' est-il lance ?" }

Write-Host "Sauvegarde terminee : $file" -ForegroundColor Green
