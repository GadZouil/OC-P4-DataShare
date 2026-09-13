# DataShare — Plateforme de transfert sécurisé de fichiers

Prototype (MVP) de plateforme de transfert de fichiers pour freelances et petites entreprises : téléversement, lien de téléchargement temporaire, mot de passe optionnel, expiration automatique.

Projet 4 de la formation OpenClassrooms **Architecte logiciel** — mission « Pilotez le développement d'une solution informatique » (DataShare, responsable produit : Lisa).

| | |
|---|---|
| **Dépôt** | https://github.com/GadZouil/OC-P4-DataShare (branche `develop`) |
| **Documentation technique** | [`docs/documentation_technique.pdf`](./docs/documentation_technique.pdf) |
| **Contrat d'API** | [`docs/openapi.yaml`](./docs/openapi.yaml) · [`docs/API_REFERENCE.md`](./docs/API_REFERENCE.md) |
| **Qualité & maintenance** | [TESTING.md](./TESTING.md) · [SECURITY.md](./SECURITY.md) · [PERF.md](./PERF.md) · [MAINTENANCE.md](./MAINTENANCE.md) |
| **Usage de l'IA** | [AI_USAGE.md](./AI_USAGE.md) (+ prompt tracé dans [`IA_Instructions.txt`](./IA_Instructions.txt)) |

---

## Fonctionnalités

| | User story | Statut |
|---|---|---|
| US01 | Téléverser un fichier (utilisateur connecté), 1 Go max | ✅ |
| US02 | Télécharger via un lien unique (sans compte) | ✅ |
| US03 | Créer un compte | ✅ |
| US04 | Se connecter (JWT) | ✅ |
| US05 | Consulter l'historique de ses fichiers (actifs / expirés) | ✅ |
| US06 | Supprimer un fichier (le lien devient invalide) | ✅ |
| US07 | Téléverser sans compte (upload anonyme) | ✅ avancée |
| US08 | Tags à l'upload + filtre dans « Mon espace » — *développée par le copilote IA sous supervision* | ✅ avancée |
| US09 | Protéger un fichier par mot de passe | ✅ avancée |
| US10 | Expiration automatique (1 à 7 jours) et purge quotidienne | ✅ avancée |

Écrans conformes aux maquettes Figma fournies (accueil / upload, connexion, inscription, « Mon espace », page de téléchargement), responsive mobile.

---

## Stack technique

| Couche | Choix | Version |
|---|---|---|
| Frontend | Vue 3 (Composition API) + TypeScript + Vite, Pinia, Vue Router, Axios | Vue 3.5 · Vite 7 |
| Backend | ASP.NET Core Web API (C#), Entity Framework Core, ASP.NET Core Identity, JWT Bearer | .NET 9 |
| Base de données | PostgreSQL (tags en `text[]` natif) | 16 |
| Stockage des fichiers | Système de fichiers local derrière une interface `IFileStorage` (migration S3 possible sans toucher aux contrôleurs) | — |
| Infra | Docker Compose : `db` + `api` + `frontend` (nginx sert la SPA et proxifie `/api`) | — |
| Tests | xUnit + FluentAssertions + Moq (41 tests), Cypress (5 scénarios E2E), k6 (charge) | — |

Justification détaillée des choix : documentation technique, section 2.

---

## Installation et lancement

### Prérequis

- **Docker Desktop** (Docker Compose v2) — suffit pour l'option A
- Pour le développement local (option B) : **.NET SDK 9.0** et **Node.js 20+**

### Option A — Tout en Docker (recommandé, 1 commande)

```bash
git clone https://github.com/GadZouil/OC-P4-DataShare.git
cd OC-P4-DataShare
cp .env.example .env            # optionnel : adapter mot de passe BDD / clé JWT (valeurs de démo par défaut)

docker compose up -d --build    # ou : ./scripts/deploy.sh   |   .\scripts\deploy.ps1
```

| Service | URL |
|---|---|
| Application | http://localhost |
| API | http://localhost:5000 — santé : `GET /api/health` |
| PostgreSQL | `localhost:5433`, base `datashare` |

La base et son schéma sont créés automatiquement : le conteneur PostgreSQL initialise la base, l'API applique les **migrations EF Core au démarrage**. Aucune commande SQL à lancer. Les fichiers uploadés et les données persistent dans des volumes Docker (`docker compose down -v` pour tout effacer).

### Option B — Développement local (base en Docker, API et front en local)

```bash
# 1. Base de données seule
docker compose up -d db

# 2. API (une seule fois : chaîne de connexion en user-secrets, jamais dans le repo)
cd backend/DataShare.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=127.0.0.1;Port=5433;Database=datashare;Username=datashare;Password=datashare"
dotnet run                       # http://localhost:5180 — Swagger UI : http://localhost:5180/swagger

# 3. Front
cd frontend/datashare-front
npm ci
npm run dev                      # http://localhost:5173 (CORS autorisé par défaut pour cette origine)
```

### Utilisation

1. Ouvrir http://localhost, cliquer sur le bouton d'upload et choisir un fichier (possible **sans compte**).
2. Choisir l'expiration (1–7 jours), un mot de passe (optionnel), des tags (optionnel), puis « Téléverser ».
3. Copier le lien `http://localhost/download/<token>` et l'envoyer au destinataire : il voit le nom et la taille du fichier, saisit le mot de passe si besoin, télécharge.
4. Avec un compte (« Se connecter » → « Créer un compte ») : « Mon espace » liste les fichiers, filtre par tag et statut, permet de copier le lien ou de supprimer.

---

## Tests et qualité

```bash
cd backend && dotnet test                                   # 41 tests unitaires + intégration
cd frontend/datashare-front && npm run build && npm run lint # type-check strict + ESLint
cd frontend && npx cypress run                              # E2E contre http://localhost (stack Docker lancée)
k6 run perf/k6-upload-test.js                               # test de charge (API Docker)
```

Sous Windows, `.\scripts\verify.ps1` enchaîne tests, couverture, build front et audits de sécurité, et affiche un résumé.

Résultats et détails : [TESTING.md](./TESTING.md) (plan de tests, couverture 83 % du code métier), [SECURITY.md](./SECURITY.md) (audit des dépendances, mécanismes en place), [PERF.md](./PERF.md) (k6, Lighthouse, logs structurés), [MAINTENANCE.md](./MAINTENANCE.md) (exploitation, sauvegardes, mises à jour).

---

## Structure du dépôt

```
OC-P4-DataShare/
├── backend/
│   ├── DataShare.Api/            # API ASP.NET Core 9
│   │   ├── Controllers/          # AuthController, FilesController, PublicFilesController
│   │   ├── Services/             # IFileStorage / LocalFileStorage, ExpiredFilesCleanupService
│   │   ├── Middleware/           # RequestMetricsMiddleware (logs structurés par requête)
│   │   ├── Models/, Data/        # AppUser, FileItem, DataShareDbContext
│   │   ├── Migrations/           # migrations EF Core (appliquées au démarrage)
│   │   └── Program.cs            # pipeline, Identity, JWT, CORS, logs
│   ├── DataShare.Api.Tests/      # xUnit : UnitTests/, IntegrationTests/, Helpers/
│   └── Dockerfile
├── frontend/
│   ├── datashare-front/          # SPA Vue 3 (src/views, src/api, src/services, src/router, src/styles)
│   ├── cypress/e2e/              # 5 scénarios end-to-end
│   ├── nginx.conf, Dockerfile    # front en production : nginx + proxy /api
├── docs/
│   ├── documentation_technique.pdf (+ .tex)   # documentation technique complète
│   ├── openapi.yaml, API_REFERENCE.md         # contrat d'interface
│   ├── diagrams/                              # architecture, MCD, flux (PlantUML + PNG, draw.io)
│   ├── coverage-report.png, lighthouse-*.png  # preuves qualité
│   └── soutenance/                            # support de présentation, script oral, scénario de démo
├── perf/                         # scripts k6 (upload, login)
├── scripts/                      # deploy.ps1 / deploy.sh, db-backup.ps1, db-restore.ps1, verify.ps1
├── docker-compose.yml, .env.example
├── AI_USAGE.md, IA_Instructions.txt
└── TESTING.md, SECURITY.md, PERF.md, MAINTENANCE.md
```

## Historique Git

Commits au format **Conventional Commits** (`feat`, `fix`, `docs`, `test`, `chore`, `perf`) ; les contributions du copilote IA sur l'US08 sont isolées (`feat(ai): add tags UI and filtering`) puis relues (`fix(tags): show clear UI errors ...`). Branches : `main` (livraisons), `develop` (intégration).

## Auteur

Ethan Legros — référent technique senior DataShare (mise en situation), OpenClassrooms, parcours Architecte logiciel.
