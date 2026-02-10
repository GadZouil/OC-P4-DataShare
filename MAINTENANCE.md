# Guide de Maintenance & Opérations - DataShare API

Ce document détaille les procédures techniques pour assurer le bon fonctionnement, la surveillance et la mise à jour de l'API Backend DataShare.

## 1. Gestion du Service (Docker)

L'application est entièrement conteneurisée. Voici les commandes pour gérer le cycle de vie de l'application.

### Démarrage et Arrêt
*   **Lancer l'environnement :**
    Cette commande construit les images si nécessaire et lance les conteneurs en arrière-plan.
    ```bash
    docker-compose up -d --build
    ```

*   **Arrêter l'environnement :**
    Arrête et supprime les conteneurs, réseaux et volumes temporaires.
    ```bash
    docker-compose down
    ```

*   **Voir les logs en temps réel :**
    ```bash
    docker logs -f datashare-api
    ```

## 2. Base de Données (PostgreSQL)

Les données sont persistées dans un volume Docker nommé `postgres_data`.

### Sauvegarde (Backup)
Pour effectuer une sauvegarde "à chaud" de la base de données sans arrêter le service :

```bash
# Exporter la base 'DataShareDb' dans un fichier backup.sql
docker exec -t datashare-db pg_dump -U postgres DataShareDb > backup_$(date +%F).sql
