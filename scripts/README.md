# Scripts de déploiement — DataShare

Scripts d'installation, de configuration et d'exploitation de la base de données et de l'application.

| Script | Plateforme | Rôle |
|--------|-----------|------|
| `deploy.ps1` / `deploy.sh` | Windows / Linux-macOS | Déploiement complet : vérifie les prérequis, build les images Docker, lance PostgreSQL + API + frontend, attend que l'API réponde. Option `-Reset` / `--reset` pour repartir d'une base vierge. |
| `db-backup.ps1` | Windows | Sauvegarde la base PostgreSQL (`pg_dump`) dans un fichier SQL daté. |
| `db-restore.ps1` | Windows | Restaure la base depuis un fichier de sauvegarde. |

## Configuration de la base de données

Aucun script SQL manuel n'est nécessaire :

1. Le conteneur PostgreSQL crée la base `datashare` et son utilisateur au premier démarrage (variables d'environnement du `docker-compose.yml`).
2. Le schéma est géré par les **migrations Entity Framework Core**, appliquées automatiquement au démarrage de l'API (`Database.MigrateAsync()` dans `Program.cs`).

Pour un environnement de développement local (backend hors Docker), voir le README racine — section « Développement local ».
