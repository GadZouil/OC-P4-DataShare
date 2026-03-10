# DataShare — Plateforme de partage de fichiers

Application full stack de partage de fichiers sécurisé, développée dans le cadre du Projet 4 de la formation OpenClassrooms Développeur Full Stack.

## Stack technique

| Couche | Technologie |
|--------|------------|
| Frontend | Vue 3 (Composition API) + TypeScript + Vite |
| Backend | ASP.NET Core 8 (C#) — API REST |
| Base de données | PostgreSQL 16 (Docker) |
| Authentification | JWT Bearer |
| Tests backend | xUnit + Moq |
| Tests e2e | Cypress |

## Installation rapide

### Prérequis
- .NET 8 SDK
- Node.js 18+
- Docker & Docker Compose

### Lancement

```bash
# 1. Base de données
docker-compose up -d

# 2. Backend
cd backend/DataShareAPI
dotnet ef database update
dotnet run

# 3. Frontend
cd frontend/datashare-front
npm install
npm run dev
```

L'application est accessible sur http://localhost:5173.

## Structure du projet

```
DataShare/
├── backend/DataShareAPI/     # API REST .NET 8
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   └── Tests/
├── frontend/datashare-front/ # SPA Vue 3
│   ├── src/views/
│   ├── src/api/
│   ├── src/stores/
│   └── cypress/
├── docker-compose.yml        # PostgreSQL
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
cd backend/DataShareAPI && dotnet test

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
