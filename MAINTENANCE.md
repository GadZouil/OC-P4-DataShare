# Maintenance & exploitation — DataShare

Procédures pour faire tourner, sauvegarder, mettre à jour et corriger le prototype. Public visé : un développeur qui reprend le projet.

---

## 1. Exploitation (Docker Compose)

La stack complète (PostgreSQL, API, front nginx) se pilote depuis la racine du repo. Les secrets sont lus dans un fichier `.env` (copie de `.env.example`, jamais versionné).

```bash
docker compose up -d --build      # construire et lancer les 3 services
docker compose ps                 # état + healthchecks (db, api)
docker compose logs -f api        # logs structurés JSON de l'API
docker compose down               # arrêter (les données et les fichiers sont conservés dans les volumes)
docker compose down -v            # arrêter ET effacer base + fichiers uploadés (reset complet)
```

Scripts équivalents : `scripts/deploy.ps1` / `scripts/deploy.sh` (vérifient les prérequis, lancent la stack, attendent `GET /api/health`) — option `-Reset` / `--reset` pour repartir d'une base vierge.

| Service | Conteneur | Port hôte | Persistance |
|---|---|---|---|
| PostgreSQL 16 | `oc-p4-datashare-postgres` | 5433 | volume `datashare_pg` |
| API ASP.NET Core 9 | `oc-p4-datashare-api` | 5000 | volume `datashare_uploads` (fichiers, `/app/Storage/Uploads`) |
| Front (nginx) | `oc-p4-datashare-frontend` | 80 | — |

**Migrations de schéma** : appliquées automatiquement au démarrage de l'API (`Database.MigrateAsync()` dans `Program.cs`). Aucune commande manuelle après un `docker compose up` ou un `--reset`. Pour créer une nouvelle migration en développement : `cd backend/DataShare.Api && dotnet ef migrations add <Nom>`.

### Développement local (hors Docker)

```bash
docker compose up -d db                      # base seule (port 5433)
cd backend/DataShare.Api && dotnet run       # API : http://localhost:5180 (Swagger : /swagger), migrations auto
cd frontend/datashare-front && npm run dev   # front : http://localhost:5173
```

---

## 2. Sauvegarde et restauration

### Base de données

```powershell
.\scripts\db-backup.ps1                                   # -> backups\datashare_backup_<date>.sql
.\scripts\db-restore.ps1 -BackupFile .\backups\datashare_backup_2026-09-13_100000.sql
```

Équivalent Linux/macOS :

```bash
docker exec -t oc-p4-datashare-postgres pg_dump -U datashare datashare > backup_$(date +%F).sql
docker exec -i oc-p4-datashare-postgres psql -U datashare datashare < backup_2026-09-13.sql
```

### Fichiers uploadés

Les fichiers vivent dans le volume `datashare_uploads`. Sauvegarde/restauration du volume :

```bash
docker run --rm -v oc-p4-datashare_datashare_uploads:/data -v "$PWD/backups:/backup" alpine tar czf /backup/uploads_$(date +%F).tgz -C /data .
docker run --rm -v oc-p4-datashare_datashare_uploads:/data -v "$PWD/backups:/backup" alpine tar xzf /backup/uploads_2026-09-13.tgz -C /data
```

> Base et fichiers doivent être sauvegardés **ensemble** (les métadonnées référencent les noms stockés). Fréquence recommandée : quotidienne en production, avant chaque mise à jour majeure.

---

## 3. Mise à jour des dépendances

### Procédure

| Étape | Backend (.NET) | Frontend (npm) |
|---|---|---|
| 1. Inventorier | `dotnet list package --outdated` | `npm outdated` |
| 2. Auditer | `dotnet list package --vulnerable --include-transitive` | `npm audit` |
| 3. Mettre à jour | `dotnet add package <Nom> --version <x.y.z>` | `npm update` (patch/mineur) ou `npm install <pkg>@<version>` |
| 4. Vérifier | `dotnet build` puis `dotnet test` | `npm run build` (type-check) puis `npm run lint` ; Cypress sur la stack Docker |
| 5. Tracer | Commit `chore(deps): ...` ou `fix(security): ...`, ligne dans [SECURITY.md](./SECURITY.md) § 4 si vulnérabilité | idem |

Les deux `package-lock.json` (`frontend/datashare-front` pour l'app, `frontend` pour Cypress) sont versionnés : toujours installer avec `npm ci` pour reproduire exactement l'arbre de dépendances.

### Fréquence

| Quoi | Quand |
|---|---|
| Correctifs de sécurité (`npm audit`, `dotnet list package --vulnerable`) | **Immédiatement** dès publication d'un avis ; contrôle à chaque livraison |
| Mises à jour patch / mineures | Mensuel |
| Mises à jour majeures (.NET, Vite, Vue, PostgreSQL) | Planifiées, 1 à 2 fois par an, sur branche dédiée |
| Image `postgres` | Suivre les versions mineures (16.x) ; changement de majeure = migration de données |

### Risques et parades

| Type de mise à jour | Risque | Parade |
|---|---|---|
| Patch (`9.0.1` → `9.0.2`) | Très faible | `dotnet test` / `npm run build` |
| Mineure (`9.0` → `9.1`) | Faible : dépréciations, typages durcis | Lire le changelog, suite de tests complète |
| Majeure (.NET 9 → 10, Vite 7 → 8, Vue 3 → 4) | Élevé : API supprimées, breaking changes | Branche dédiée, guide de migration officiel, tests complets, retour arrière par Git |
| `npm audit fix --force` | Élevé : installe des majeures non testées | **Interdit** sans revue ; préférer des mises à jour ciblées |
| Image PostgreSQL majeure (16 → 17) | Format de données incompatible | `pg_dump` avant, `pg_restore` après, jamais de changement de majeure sur un volume existant |
| Cypress (binaire) | Téléchargement à l'installation, cache local | `CYPRESS_INSTALL_BINARY=0` pour un `npm ci` sans binaire (CI, audit) |

**Cas concrets rencontrés** : (08/2026) la mise à jour de sécurité d'`axios` a durci le typage des en-têtes et cassé le type-check — détecté par `npm run build`, corrigé en une ligne. (09/2026) `npm audit fix` a mis Cypress 15.10 → 15.21 sans impact sur les scénarios. C'est exactement le rôle du filet tests + typage.

---

## 4. Diagnostic et correction de bugs

### Où regarder

| Symptôme | Où | Commande |
|---|---|---|
| Erreur API (500, lenteur) | Logs structurés de l'API (une ligne par requête : route, statut, durée) | `docker compose logs -f api` — filtrer `"LogLevel":"Error"` |
| API ne démarre pas | Message explicite au démarrage (ex. `Jwt:Key` manquante ou < 32 caractères, base injoignable) | `docker compose logs api` |
| Base injoignable | Healthcheck PostgreSQL | `docker compose ps`, `docker logs oc-p4-datashare-postgres` |
| Front : page blanche, appel API en échec | Console navigateur (F12) → onglets Console et Network | — |
| Lien de partage « invalide ou expiré » | Fichier supprimé, expiré (`410`) ou token faux (`404`) | `docker compose logs api | grep <token>` |

### Procédure

1. **Reproduire** le bug, idéalement par un test qui échoue (`backend/DataShare.Api.Tests` ou `frontend/cypress/e2e`).
2. **Isoler** la couche : front (console), API (logs structurés), base (logs PostgreSQL).
3. **Corriger** sur une branche `fix/<description-courte>` avec un commit conventionnel (`fix(scope): ...`).
4. **Vérifier** : `dotnet test`, `npm run build`, scénario Cypress concerné.
5. **Fusionner** dans `develop` après revue, puis dans `main` pour livrer.

### Convention de branches et de commits

- `main` : versions livrées ; `develop` : intégration ; `feat/...`, `fix/...`, `docs/...` : travail en cours.
- Messages au format **Conventional Commits** (`feat`, `fix`, `docs`, `test`, `chore`, `perf`, `refactor`) avec scope (`api`, `front`, `docker`, `deploy`, `security`, `soutenance`). Les contributions du copilote IA sont isolées (`feat(ai): ...`) puis relues (`fix: ... (revue humaine)`), voir [AI_USAGE.md](./AI_USAGE.md).

---

## 5. Fichiers uploadés et purge

- Durée de vie : 1 à 7 jours (choisie à l'upload, 7 par défaut).
- Purge automatique : `ExpiredFilesCleanupService` (tâche de fond) supprime disque + métadonnées au démarrage de l'API puis toutes les 24 h ; les liens expirés répondent `410` en attendant.
- Contrôle manuel de l'espace utilisé : `docker exec oc-p4-datashare-api du -sh /app/Storage/Uploads`.
- Fichiers orphelins (présents sur disque sans ligne en base, ex. après une restauration partielle) : comparer `ls /app/Storage/Uploads` avec `SELECT "StoredFileName" FROM "Files"` et supprimer les écarts.

---

## 6. Checklist de mise en production

- [ ] `.env` renseigné : mot de passe PostgreSQL fort, `JWT_KEY` aléatoire ≥ 32 caractères, `CORS_ALLOWED_ORIGINS` = domaine du front
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (défaut de l'image ; désactive Swagger, active les logs JSON)
- [ ] HTTPS terminé sur le reverse proxy (certificat), voir [SECURITY.md](./SECURITY.md) § 6
- [ ] `dotnet test` vert, `npm run build` + `npm run lint` verts, scénarios Cypress passés sur la stack cible
- [ ] `dotnet list package --vulnerable` et `npm audit` : 0 vulnérabilité (ou décision documentée)
- [ ] Sauvegarde base + volume `datashare_uploads` réalisée et restauration testée
- [ ] `GET /api/health` répond `{"status":"ok"}` derrière le proxy
