# Guide de Maintenance & Opérations — DataShare API

## 1. Gestion du Service (Docker)

L'application complète (base de données, API backend, frontend) se lance via Docker Compose.

### Lancement complet

```bash
docker-compose up -d        # Lance tout (db, api, frontend)
docker-compose logs -f api  # Voir les logs API
docker-compose down         # Arrêter tous les services
```

### Démarrage et Arrêt (détail par service)

```bash
# Lancer uniquement la base de données
docker-compose up -d db

# Voir les logs PostgreSQL
docker logs -f oc-p4-datashare-postgres

# Lancer le backend en développement local (hors Docker)
cd backend/DataShare.Api && dotnet run

# Lancer le frontend en développement local (hors Docker)
cd frontend/datashare-front && npm run dev
```

## 2. Base de Données (PostgreSQL)

Les données sont persistées dans un volume Docker nommé `datashare_pg`.

### Sauvegarde (Backup)

```bash
# Exporter la base dans un fichier daté
docker exec -t oc-p4-datashare-postgres pg_dump -U datashare datashare > backup_$(date +%F).sql
```

### Restauration (Restore)

```bash
# Restaurer depuis un fichier backup
docker exec -i oc-p4-datashare-postgres psql -U datashare datashare < backup_2025-01-15.sql
```

### Réinitialisation complète

```bash
docker-compose down -v   # Supprime le volume
docker-compose up -d     # Recrée la base vierge
cd backend/DataShare.Api && dotnet ef database update  # Applique les migrations
```

> Fréquence recommandée : backup quotidien en production, hebdomadaire en staging.

## 3. Mise à jour des dépendances

### Backend (.NET / NuGet)

```bash
cd backend/DataShare.Api

# Lister les packages obsolètes
dotnet list package --outdated

# Mettre à jour un package spécifique
dotnet add package <NomPackage>

# Après mise à jour : lancer les tests
dotnet test
```

> Fréquence : vérification mensuelle. Mises à jour de sécurité immédiatement.

### Frontend (npm)

```bash
cd frontend/datashare-front

# Audit de sécurité
npm audit

# Corriger automatiquement les vulnérabilités mineures
npm audit fix

# Lister les packages obsolètes
npm outdated

# Mettre à jour
npm update
```

> Fréquence : `npm audit` à chaque sprint. `npm outdated` mensuel.

### Base de données (PostgreSQL)

```bash
# Vérifier la version actuelle
docker exec oc-p4-datashare-postgres psql -U datashare -c "SELECT version();"

# Pour upgrader : backup, modifier la version dans docker-compose.yml, restore
```

> Fréquence : suivre les releases PostgreSQL, upgrader pour les correctifs de sécurité.

## 4. Procédure de correction de bugs

1. Reproduire le bug (idéalement écrire un test qui échoue)
2. Isoler : backend ? frontend ? base de données ?
3. Corriger sur une branche dédiée (`fix/description-courte`)
4. Vérifier : lancer `cd backend && dotnet test` + `cd frontend/datashare-front && npx cypress run`
5. Merger après revue et tests verts

### Logs utiles pour le diagnostic

```bash
# Logs backend .NET (en mode développement)
cd backend/DataShare.Api && dotnet run
# Les logs s'affichent dans la console (Serilog/console logger)

# Logs PostgreSQL
docker logs oc-p4-datashare-postgres --tail 100

# Logs frontend (erreurs runtime)
# Ouvrir la console navigateur (F12)
```

## 5. Gestion des fichiers uploadés

Les fichiers sont stockés sur le système de fichiers du serveur backend (dossier configuré dans `appsettings.json`).

### Nettoyage des fichiers expirés

Les fichiers ont une durée de vie maximale de 7 jours (US10). Le nettoyage peut être :

- **Automatique** : via le service d'expiration intégré au backend
- **Manuel** : vérifier et purger les fichiers orphelins

```bash
# Vérifier l'espace disque utilisé par les uploads
du -sh backend/DataShare.Api/Storage/Uploads/
```

## 6. Checklist de mise en production

- [ ] Variables d'environnement configurées (JWT secret, connection string)
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] HTTPS activé
- [ ] Backup base de données effectué
- [ ] `dotnet test` — tous les tests passent
- [ ] `npm audit` — aucune vulnérabilité critique
- [ ] Fichiers statiques frontend buildés (`npm run build`)
