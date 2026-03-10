# DataShare — Plateforme de transfert sécurisé de fichiers

Application web permettant à des utilisateurs (anonymes ou enregistrés) de transférer des fichiers via des liens de téléchargement temporaires, avec options de protection et gestion pour les utilisateurs connectés.

## Stack technique

| Composant | Technologie |
|-----------|------------|
| Backend | ASP.NET Core 9 (C#) |
| Frontend | Vue 3 + TypeScript + Vite |
| Base de données | PostgreSQL 16 |
| Stockage | Système de fichiers local |
| Auth | JWT (JSON Web Tokens) |
| Tests backend | xUnit + FluentAssertions + Moq |
| Tests e2e | Cypress |
| Performance | k6 |

## Prérequis

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/) et npm
- [Docker](https://www.docker.com/) et Docker Compose
- [Git](https://git-scm.com/)

## Installation et lancement

### 1. Cloner le repo

```bash
git clone <url-du-repo>
cd datashare
```

### 2. Lancer la base de données

```bash
docker-compose up -d
```

PostgreSQL sera accessible sur `localhost:5433`.

### 3. Lancer le backend

```bash
cd backend/DataShareAPI
dotnet restore
dotnet ef database update  # Appliquer les migrations
dotnet run
```

L'API sera accessible sur `https://localhost:5001` (ou le port configuré).

### 4. Lancer le frontend

```bash
cd frontend/datashare-front
npm install
npm run dev
```

L'application sera accessible sur `http://localhost:5173`.

## Variables d'environnement

### Backend (User Secrets ou variables d'env)

| Variable | Description | Exemple |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Connexion PostgreSQL | `Host=localhost;Port=5433;Database=datashare;Username=datashare;Password=datashare` |
| `Jwt__Key` | Clé secrète JWT | `une-cle-secrete-de-32-caracteres-min` |
| `Jwt__Issuer` | Émetteur JWT | `DataShareAPI` |
| `Jwt__Audience` | Audience JWT | `DataShareFront` |

### Frontend

| Variable | Description | Exemple |
|----------|-------------|---------|
| `VITE_API_URL` | URL de l'API backend | `https://localhost:5001/api` |

## Structure du projet

```
datashare/
├── backend/
│   └── DataShareAPI/         # API .NET 9
│       ├── Controllers/      # Endpoints REST
│       ├── Models/           # Entités et DTOs
│       ├── Services/         # Logique métier
│       └── Tests/            # Tests xUnit
├── frontend/
│   └── datashare-front/      # App Vue 3
│       ├── src/
│       │   ├── views/        # Pages (Upload, Me, Login, Register)
│       │   ├── api/          # Clients API
│       │   ├── stores/       # Pinia stores
│       │   └── styles/       # CSS
│       └── cypress/          # Tests e2e
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
