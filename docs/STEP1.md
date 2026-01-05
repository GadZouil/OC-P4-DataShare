# Étape 1 — Architecture, MCD, contrat d’interface

## Livrables
- Diagramme d’architecture : `docs/diagrams/architecture.png` (source : `docs/diagrams/architecture.drawio`)
- MCD / Modèle de données : `docs/diagrams/mcd.png` (source : `docs/diagrams/mcd.drawio`)
- Contrat d’interface (OpenAPI) : `docs/openapi.yaml`

## Stack retenue
- Back-end : ASP.NET Core Web API (.NET)
- Front-end : Vue 3 (SPA)
- Base de données : PostgreSQL
- Stockage : local (système de fichiers)

## Règles fonctionnelles (MVP)
- Upload authentifié (JWT) vers stockage local, taille max 1 Go
- Lien public basé sur un token non prédictible
- Expiration configurable 1–7 jours (défaut : 7 jours)
- Mot de passe optionnel (min 6) ; si présent, requis au téléchargement
- Tâche planifiée quotidienne : purge des fichiers expirés + suppression des métadonnées

## Remarques de conception
- Les fichiers ne sont jamais stockés dans la base : seuls les métadonnées et le chemin local (`storage_path`) y figurent
- Les routes “Mon espace” sont protégées (JWT) ; les routes publiques utilisent le `token`
- Les erreurs côté lien public doivent être explicites : token invalide (404) / expiré (410)
