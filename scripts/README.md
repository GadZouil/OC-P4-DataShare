# Scripts de déploiement et d'exploitation — DataShare

| Script | Plateforme | Rôle |
|---|---|---|
| `deploy.ps1` / `deploy.sh` | Windows / Linux-macOS | Déploiement complet : vérifie Docker, construit les images, lance PostgreSQL + API + front, attend `GET /api/health`. Option `-Reset` / `--reset` pour repartir d'une base et d'un stockage vierges (`docker compose down -v`). |
| `db-backup.ps1` | Windows | Sauvegarde PostgreSQL (`pg_dump`) dans `backups/datashare_backup_<date>.sql` (dossier ignoré par Git). |
| `db-restore.ps1` | Windows | Restauration depuis un fichier de sauvegarde. |
| `verify.ps1` | Windows | Vérification qualité avant livraison : `dotnet test` + couverture (rapport HTML), `npm run build` + `npm run lint`, `npm audit`, `dotnet list package --vulnerable`. Affiche un résumé. |

## Installation et configuration de la base de données

Aucun script SQL manuel :

1. Le conteneur PostgreSQL (`docker-compose.yml`, service `db`) crée la base `datashare` et son utilisateur au premier démarrage à partir des variables `POSTGRES_*` du fichier `.env` (copie de `.env.example` ; valeurs de démo par défaut).
2. Le schéma est géré par les **migrations Entity Framework Core** (`backend/DataShare.Api/Migrations`), appliquées automatiquement au démarrage de l'API (`Database.MigrateAsync()` dans `Program.cs`).

Les données sont conservées dans le volume `datashare_pg`, les fichiers uploadés dans `datashare_uploads`.

Pour un environnement de développement local (API hors Docker), voir le README racine — option B.
