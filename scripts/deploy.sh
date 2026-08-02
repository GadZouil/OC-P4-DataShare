#!/usr/bin/env bash
# =============================================================================
# DataShare - Script de deploiement complet (Linux / macOS)
# Installe et lance l'application : PostgreSQL + API + Frontend via Docker.
# Les migrations EF Core sont appliquees automatiquement au demarrage de l'API.
#
# Usage :   ./scripts/deploy.sh            # deploiement standard
#           ./scripts/deploy.sh --reset    # reinitialise la base de donnees
# =============================================================================
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> Verification des prerequis..."
command -v docker >/dev/null 2>&1 || { echo "Docker n'est pas installe. https://docs.docker.com/get-docker/"; exit 1; }
docker info >/dev/null 2>&1 || { echo "Le demon Docker ne repond pas."; exit 1; }

if [[ "${1:-}" == "--reset" ]]; then
    echo "==> Reinitialisation : suppression des conteneurs et du volume BDD..."
    docker compose down -v
fi

echo "==> Build et lancement de la stack (db + api + frontend)..."
docker compose up -d --build

echo "==> Attente de la disponibilite de l'API..."
for i in $(seq 1 30); do
    if curl -fsS http://localhost:5000/api/health >/dev/null 2>&1; then
        echo ""
        echo "Deploiement termine avec succes."
        echo "  Frontend  : http://localhost"
        echo "  API       : http://localhost:5000  (sante : /api/health)"
        echo "  PostgreSQL: localhost:5433 (base 'datashare')"
        echo ""
        echo "Arret : docker compose down    (ajouter -v pour effacer les donnees)"
        exit 0
    fi
    sleep 3
done

echo "L'API ne repond pas apres 90s. Consultez les logs : docker compose logs api"
exit 1
