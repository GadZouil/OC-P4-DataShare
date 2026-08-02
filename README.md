# DataShare — Plateforme de partage de fichiers

Application full stack de partage de fichiers sécurisé, développée dans le cadre du Projet 4 de la formation OpenClassrooms Développeur Full Stack.

## Stack technique

| Couche | Technologie |
|--------|------------|
| Frontend | Vue 3 (Composition API) + TypeScript + Vite |
| Backend | ASP.NET Core 9 (C#) — API REST |
| Base de données | PostgreSQL 16 (Docker) |
| Authentification | JWT Bearer |
| Tests backend | xUnit + Moq |
| Tests e2e | Cypress |

## Installation rapide

### Prérequis
- .NET 9 SDK (ou SDK plus récent avec `DOTNET_ROLL_FORWARD=LatestMajor`)
- Node.js 20+
- Docker & Docker Compose

### Lancement

**Option A — Tout en Docker (recommandé)**

```bash
docker compose up -d --build
# ou via le script de déploiement :
./scripts/deploy.sh        # Linux / macOS
.\scripts\deploy.ps1       # Windows
```

- Frontend : http://localhost (port 80)
- API : http://localhost:5000 (santé : `GET /api/health`)
- PostgreSQL : port hôte 5433

Les migrations EF Core sont appliquées automatiquement au démarrage de l'API : aucune commande BDD à exécuter.

**Option B — Développement local (base de données en Docker, backend et frontend en local)**

```bash
# 1. Base de données uniquement
docker compose up -d db

# 2. Backend — configurer une seule fois la chaîne de connexion (user-secrets)
cd backend/DataShare.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=127.0.0.1;Port=5433;Database=datashare;Username=datashare;Password=datashare"
dotnet run
# API disponible sur http://localhost:5180 (Swagger : /swagger)

# 3. Frontend
cd frontend/datashare-front
npm install
npm run dev
# Frontend disponible sur http://localhost:5173
```

> Les migrations sont appliquées automatiquement au démarrage (`dotnet ef database update` n'est pas nécessaire).

## Structure du projet

```
DataShare/
├── backend/DataShare.Api/    # API REST .NET 9
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   └── DataShare.Api.Tests/
├── frontend/datashare-front/ # SPA Vue 3
│   ├── src/views/
│   ├── src/api/
│   ├── src/stores/
│   └── cypress/
├── docs/                     # Documentation technique, OpenAPI, diagrammes
├── scripts/                  # Scripts de déploiement et de gestion BDD
├── perf/                     # Script de test de charge k6
├── docker-compose.yml        # Base de données + API + Frontend (Docker)
├── AI_USAGE.md              # Documentation usage IA
├── TESTING.md               # Plan et résultats de tests
├── SECURITY.md              # Audit de sécurité
├── PERF.md                  # Tests de performance
├── MAINTENANCE.md           # Procédures de maintenance
└── README.md                # Ce fichier
```

## Fonctionnalités

### MVP

- **US01** — Upload de fichiers (utilisateur connecté)
- **US02** — Téléchargement via lien unique
- **US03** — Création de compte
- **US04** — Connexion / authentification JWT
- **US05** — Historique des fichiers uploadés
- **US06** — Suppression de fichiers

### Fonctionnalités avancées

- **US07** — Upload anonyme (sans compte)
- **US08** — Gestion des tags (implémentée via IA — voir [AI_USAGE.md](./AI_USAGE.md))
- **US09** — Protection par mot de passe
- **US10** — Expiration automatique des fichiers (7 jours max)

## Tests

```bash
# Tests unitaires + intégration backend
cd backend && dotnet test

# Tests e2e frontend
cd frontend/datashare-front && npx cypress run

# Couverture de code
dotnet test --collect:"XPlat Code Coverage"
```

## Documentation

| Document | Description |
|----------|-------------|
| [TESTING.md](./TESTING.md) | Plan de tests, couverture, instructions |
| [SECURITY.md](./SECURITY.md) | Audit sécurité, décisions |
| [PERF.md](./PERF.md) | Tests k6, budget performance, métriques |
| [MAINTENANCE.md](./MAINTENANCE.md) | Procédures maintenance, mises à jour |
| [AI_USAGE.md](./AI_USAGE.md) | Usage de l'IA dans le projet |

## Licence

Projet réalisé dans le cadre de la formation OpenClassrooms — Développeur Full Stack.
