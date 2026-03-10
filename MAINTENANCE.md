# Guide de Maintenance & Opérations — DataShare

Ce document détaille les procédures de mise à jour, sauvegarde et maintenance de l'application DataShare.

## 1. Gestion du Service (Docker)

L'application est conteneurisée via Docker Compose.

### Démarrage et Arrêt

```bash
# Lancer l'environnement
docker-compose up -d --build

# Arrêter l'environnement
docker-compose down

# Voir les logs en temps réel
docker logs -f datashare-api
```

## 2. Base de Données (PostgreSQL)

Les données sont persistées dans le volume Docker `datashare_pg`.

### Sauvegarde (Backup)

```bash
# Exporter la base dans un fichier daté
docker exec -t oc-p4-datashare-postgres pg_dump -U datashare datashare > backup_$(date +%F).sql
```

### Restauration (Restore)

```bash
# Restaurer depuis un backup
cat backup_2026-02-17.sql | docker exec -i oc-p4-datashare-postgres psql -U datashare datashare
```

### Vérification de santé

```bash
# Vérifier que PostgreSQL répond
docker exec oc-p4-datashare-postgres pg_isready -U datashare
```

## 3. Mise à jour des dépendances

### Backend (.NET / NuGet)

| Action | Commande | Fréquence |
|--------|----------|-----------|
| Lister les packages obsolètes | `dotnet list package --outdated` | Mensuelle |
| Mettre à jour un package | `dotnet add package <nom> --version <x.y.z>` | Selon criticité |
| Vérifier les vulnérabilités | `dotnet list package --vulnerable` | Hebdomadaire |

**Risques** : Les mises à jour majeures de packages (ex: Entity Framework, ASP.NET) peuvent introduire des breaking changes. Toujours :
1. Lire le changelog du package
2. Mettre à jour sur une branche dédiée
3. Lancer la suite de tests complète (`dotnet test`)
4. Vérifier le bon fonctionnement des endpoints critiques (upload, download, auth)

### Frontend (npm)

| Action | Commande | Fréquence |
|--------|----------|-----------|
| Lister les packages obsolètes | `npm outdated` | Mensuelle |
| Mettre à jour (mineures/patches) | `npm update` | Mensuelle |
| Audit de sécurité | `npm audit` | Hebdomadaire |
| Corriger les vulnérabilités | `npm audit fix` | Dès détection |

**Risques** : Les mises à jour majeures de Vue, Vite ou vue-router nécessitent une attention particulière (breaking changes API). Tester systématiquement le build (`npm run build`) et les tests e2e Cypress après mise à jour.

## 4. Procédure de correction de bugs

1. **Identifier** : Reproduire le bug, vérifier les logs (`docker logs`)
2. **Isoler** : Créer une branche `fix/<description>`
3. **Corriger** : Implémenter le fix avec test de non-régression
4. **Valider** : Lancer `dotnet test` + tests Cypress
5. **Déployer** : Merge dans main, rebuild Docker

## 5. Monitoring

### Logs applicatifs

```bash
# Logs backend
docker logs -f datashare-api --tail 100

# Logs PostgreSQL
docker logs -f oc-p4-datashare-postgres --tail 100
```

### Espace disque (fichiers uploadés)

```bash
# Vérifier la taille du dossier uploads
du -sh ./Uploads/

# Vérifier l'espace disque du volume PostgreSQL
docker system df -v
```

## 6. Procédure de mise à jour applicative

1. Pull la dernière version : `git pull origin main`
2. Rebuild les conteneurs : `docker-compose up -d --build`
3. Vérifier les migrations BDD si nécessaire : `dotnet ef database update`
4. Vérifier les logs : `docker logs -f datashare-api`
5. Test de fumée : upload + download + auth
